using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking.Unity;
using ForgeAndUnity.Unity;

namespace ForgeAndUnity.Forge {

    /// <summary>
    /// A <see cref="NetworkBehavior"/> that acts as a service for all <see cref="NodeManager"/>s to
    /// register and unregister scenes to be looked up globally by other <see cref="NodeManager"/>s.
    /// Also provides functionality to create <see cref="NetworkSceneItem"/>s and <see cref="NetworkBehavior"/>s across Nodes.
    /// </summary>
    public class NodeService : NodeServiceBehavior, INetworkSceneObject {
        //Fields
        public const float                                                          CACHE_LIFETIME_PENDING_SERVICE_CALLBACKS    = 30f;
        public const float                                                          CACHE_LIFETIME_SCENES_CACHED                = 1800f;
        public static NodeService                                                   Instance;
        public static bool                                                          IsInitialized                               { get { return Instance != null; } }

        protected HandlerPoolUInt                                                   _usedServiceCallbackIds;
        protected CacheDictionary<uint, ServiceCallback, float>                     _pendingServiceCallbacks;
        protected Dictionary<uint, Dictionary<string, NodeNetworkSceneTemplate>>    _scenesRegistered;
        protected CacheDictionary<string, NodeNetworkSceneTemplate, float>          _scenesCached;
        protected Dictionary<uint, NetworkingPlayer>                                _nodeToNetworkingPlayer;
        protected bool                                                              _enableLookupCaching;
        protected NetworkSceneManager                                               _manager;
        protected IEnumerator                                                       _updatePendingServiceCallbacks;
        protected IEnumerator                                                       _updateScenesCached;

        public HandlerPoolUInt                                                      UsedServiceCallbackIds          { get { return _usedServiceCallbackIds; } }
        public CacheDictionary<uint, ServiceCallback, float>                        PendingServiceCallbacks         { get { return _pendingServiceCallbacks; } }
        public Dictionary<uint, Dictionary<string, NodeNetworkSceneTemplate>>       ScenesRegistered                { get { return _scenesRegistered; } }
        public CacheDictionary<string, NodeNetworkSceneTemplate, float>             ScenesCached                    { get { return _scenesCached; } }
        public Dictionary<uint, NetworkingPlayer>                                   NodeToNetworkingPlayer          { get { return _nodeToNetworkingPlayer; } }
        public bool                                                                 EnableLookupCaching             { get { return _enableLookupCaching; } }
        public NetworkSceneManager                                                  Manager                         { get { return _manager; } set { _manager = value; } }

        //Events
        public delegate void RegisterNodeEvent (RpcArgs pArgs);
        public event RegisterNodeEvent OnRegisterNode;
        public delegate void RegisterSceneEvent (RpcArgs pArgs);
        public event RegisterSceneEvent OnRegisterScene;
        public delegate void UnregisterSceneEvent (RpcArgs pArgs);
        public event UnregisterSceneEvent OnUnregisterScene;
        public delegate void ConfirmSceneEvent (RpcArgs pArgs);
        public event ConfirmSceneEvent OnConfirmScene;
        public delegate void LookupSceneEvent (RpcArgs pArgs);
        public event LookupSceneEvent OnLookupScene;
        public delegate void ReceiveLookupSceneEvent (RpcArgs pArgs);
        public event ReceiveLookupSceneEvent OnReceiveLookupScene;
        public delegate void RelayInstantiateInNodeEvent (RpcArgs pArgs);
        public event RelayInstantiateInNodeEvent OnRelayInstantiateInNode;
        public delegate void InstantiateInNodeEvent (RpcArgs pArgs);
        public event InstantiateInNodeEvent OnInstantiateInNode;
        public delegate void RelayConfirmInstantiateInNodeEvent (RpcArgs pArgs);
        public event RelayConfirmInstantiateInNodeEvent OnRelayConfirmInstantiateInNode;
        public delegate void ConfirmInstantiateInNodeEvent (RpcArgs pArgs);
        public event ConfirmInstantiateInNodeEvent OnConfirmInstantiateInNode;
        public delegate void RelayCreateNetworkSceneInNodeEvent (RpcArgs pArgs);
        public event RelayCreateNetworkSceneInNodeEvent OnRelayCreateNetworkSceneInNode;
        public delegate void CreateNetworkSceneInNodeEvent (RpcArgs pArgs);
        public event CreateNetworkSceneInNodeEvent OnCreateNetworkSceneInNode;
        public delegate void RelayConfirmCreateNetworkSceneInNodeEvent (RpcArgs pArgs);
        public event RelayConfirmCreateNetworkSceneInNodeEvent OnRelayConfirmCreateNetworkSceneInNode;
        public delegate void ConfirmCreateNetworkSceneInNodeEvent (RpcArgs pArgs);
        public event ConfirmCreateNetworkSceneInNodeEvent OnConfirmCreateNetworkSceneInNode;


        //Functions
        #region Unity
        protected virtual void Awake () {
            if (Instance != null) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _usedServiceCallbackIds = new HandlerPoolUInt();
            _usedServiceCallbackIds.UseFreeIds = false; // Ids from the HandlerPoolUInt will fully count to uint.MaxValue before being reused. This is to prevent collision.
            _pendingServiceCallbacks = new CacheDictionary<uint, ServiceCallback, float>(new DelayFixedTime(GameTime.FixedTimeUpdater()), CACHE_LIFETIME_PENDING_SERVICE_CALLBACKS);
            _scenesRegistered = new Dictionary<uint, Dictionary<string, NodeNetworkSceneTemplate>>();
            _scenesCached = new CacheDictionary<string, NodeNetworkSceneTemplate, float>(new DelayFixedTime(GameTime.FixedTimeUpdater()), CACHE_LIFETIME_SCENES_CACHED);
            _nodeToNetworkingPlayer = new Dictionary<uint, NetworkingPlayer>();
            _enableLookupCaching = NodeManager.Instance.EnableLookupCaching;
            _updatePendingServiceCallbacks = _pendingServiceCallbacks.UpdateCoroutine();
            _pendingServiceCallbacks.OnCacheItemExpired += PendingServiceCallbacks_OnCacheItemExpired;
            _updateScenesCached = _scenesCached.UpdateCoroutine();
            _scenesCached.OnCacheItemExpired += ScenesCached_OnCacheItemExpired;
        }

        protected override void NetworkStart () {
            base.NetworkStart();
            RegisterEventsServerClient();
            if (networkObject.IsServer) {
                RegisterEventsServer();
            } else {
                RegisterEventsClient();
            }

            // We register this Node on the MasterNode
            RegisterNode(NodeManager.Instance.CurrentNode.NodeId);

            // Start by sending over all dynamic scenes over to the MasterNode
            foreach (var item in NodeManager.Instance.ScenesDynamic.Values) {
                NodeManager.Instance.RegisterDynamicScene(item.SceneTemplate);
            }
        }

        protected virtual void Update () {
            if (networkObject == null) {
                return;
            }

            _updatePendingServiceCallbacks.MoveNext();
            _updateScenesCached.MoveNext();
        }

        protected virtual void OnDestroy () {
            Instance = null;
            _usedServiceCallbackIds.Clear();
            foreach (var item in _pendingServiceCallbacks.CacheItems.Values) {
                item.Value.State = ServiceCallbackStateEnum.ERROR_TIMEOUT;
                item.Value.RaiseTimeout();
            }

            _pendingServiceCallbacks.Clear();
        }

        #endregion

        #region Register Events
        public virtual void RegisterEventsServer () {
            if (_manager != null && _manager.HasNetworker) {
                _manager.Networker.playerDisconnected += PlayerDisconnected_Server;
            }

            OnRegisterNode += OnRegisterNode_Server;
            OnRegisterScene += OnRegisterScene_Server;
            OnUnregisterScene += OnUnregisterScene_Server;
            OnConfirmScene += OnConfirmScene_Server;
            OnLookupScene += OnLookupScene_Server;
            OnReceiveLookupScene += OnReceiveLookupScene_Server;
            OnRelayInstantiateInNode += OnRelayInstantiateInNode_Server;
            OnInstantiateInNode += OnInstantiateInNode_Server;
            OnRelayConfirmInstantiateInNode += OnRelayConfirmInstantiateInNode_Server;
            OnConfirmInstantiateInNode += OnConfirmInstantiateInNode_Server;
            OnRelayCreateNetworkSceneInNode += OnRelayCreateNetworkSceneInNode_Server;
            OnCreateNetworkSceneInNode += OnCreateNetworkSceneInNode_Server;
            OnRelayConfirmCreateNetworkSceneInNode += OnRelayConfirmCreateNetworkSceneInNode_Server;
            OnConfirmCreateNetworkSceneInNode += OnConfirmCreateNetworkSceneInNode_Server;
        }

        public virtual void RegisterEventsClient () {
            if (_manager != null && _manager.HasNetworker) {
                _manager.Networker.disconnected += PlayerDisconnected_Client;
            }

            OnRegisterNode += OnRegisterNode_Client;
            OnRegisterScene += OnRegisterScene_Client;
            OnUnregisterScene += OnUnregisterScene_Client;
            OnConfirmScene += OnConfirmScene_Client;
            OnLookupScene += OnLookupScene_Client;
            OnReceiveLookupScene += OnReceiveLookupScene_Client;
            OnRelayInstantiateInNode += OnRelayInstantiateInNode_Client;
            OnInstantiateInNode += OnInstantiateInNode_Client;
            OnRelayConfirmInstantiateInNode += OnRelayConfirmInstantiateInNode_Client;
            OnConfirmInstantiateInNode += OnConfirmInstantiateInNode_Client;
            OnRelayCreateNetworkSceneInNode += OnRelayCreateNetworkSceneInNode_Client;
            OnCreateNetworkSceneInNode += OnCreateNetworkSceneInNode_Client;
            OnRelayConfirmCreateNetworkSceneInNode += OnRelayConfirmCreateNetworkSceneInNode_Client;
            OnConfirmCreateNetworkSceneInNode += OnConfirmCreateNetworkSceneInNode_Client;
        }

        public virtual void RegisterEventsServerClient () {
            OnRegisterNode += OnRegisterNode_ServerClient;
            OnRegisterScene += OnRegisterScene_ServerClient;
            OnUnregisterScene += OnUnregisterScene_ServerClient;
            OnConfirmScene += OnConfirmScene_ServerClient;
            OnLookupScene += OnLookupScene_ServerClient;
            OnReceiveLookupScene += OnReceiveLookupScene_ServerClient;
            OnRelayInstantiateInNode += OnRelayInstantiateInNode_ServerClient;
            OnInstantiateInNode += OnInstantiateInNode_ServerClient;
            OnRelayConfirmInstantiateInNode += OnRelayConfirmInstantiateInNode_ServerClient;
            OnConfirmInstantiateInNode += OnConfirmInstantiateInNode_ServerClient;
            OnRelayCreateNetworkSceneInNode += OnRelayCreateNetworkSceneInNode_ServerClient;
            OnCreateNetworkSceneInNode += OnCreateNetworkSceneInNode_ServerClient;
            OnRelayConfirmCreateNetworkSceneInNode += OnRelayConfirmCreateNetworkSceneInNode_ServerClient;
            OnConfirmCreateNetworkSceneInNode += OnConfirmCreateNetworkSceneInNode_ServerClient;
        }

        #endregion

        #region RegisterNode
        public static ServiceCallback<uint, ServiceCallbackStateEnum> RegisterNode (uint pSourceNodeId) {
            ServiceCallback<uint, ServiceCallbackStateEnum> callback = new ServiceCallback<uint, ServiceCallbackStateEnum>(0, pSourceNodeId, GameTime.fixedTime);
            if (!IsInitialized) {
                callback.State = ServiceCallbackStateEnum.ERROR_SERVICE_NOT_INITIALIZED;
                return callback;
            }

            if (Instance.Manager == null || !Instance.Manager.HasNetworker || !Instance.Manager.Networker.IsConnected) {
                callback.State = ServiceCallbackStateEnum.ERROR_NO_CONNECTION;
                return callback;
            }

            callback.State = ServiceCallbackStateEnum.RESPONSE_SUCCESS;
            Instance.networkObject.SendRpc(RPC_REGISTER_NODE, Receivers.Server, pSourceNodeId);
            return callback;
        }

        protected virtual void OnRegisterNode_Server (RpcArgs pArgs) {
            uint sourceNodeId = pArgs.GetNext<uint>();
            if (!_nodeToNetworkingPlayer.ContainsKey(sourceNodeId)) {
                _nodeToNetworkingPlayer.Add(sourceNodeId, pArgs.Info.SendingPlayer);
            }
        }

        protected virtual void OnRegisterNode_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnRegisterNode_ServerClient (RpcArgs pArgs) {
            // your code here...
        }

        #endregion

        #region RegisterScene
        public static ServiceCallback<RPCNetworkSceneTemplate, ServiceCallbackStateEnum> RegisterScene (uint pSourceodeId, bool pRequireConfirmation, NetworkSceneTemplate pTemplate) {
            ServiceCallback<RPCNetworkSceneTemplate, ServiceCallbackStateEnum> callback = new ServiceCallback<RPCNetworkSceneTemplate, ServiceCallbackStateEnum>(0, pSourceodeId, GameTime.fixedTime);
            if (pTemplate == null) {
                callback.State = ServiceCallbackStateEnum.ERROR_NO_DATA;
                return callback;
            }

            if (!IsInitialized) {
                callback.State = ServiceCallbackStateEnum.ERROR_SERVICE_NOT_INITIALIZED;
                return callback;
            }

            if (Instance.Manager == null || !Instance.Manager.HasNetworker || !Instance.Manager.Networker.IsConnected) {
                callback.State = ServiceCallbackStateEnum.ERROR_NO_CONNECTION;
                return callback;
            }

            callback.RequestDataOfT = pTemplate.ToRPC();
            if (pRequireConfirmation) {
                callback.State = ServiceCallbackStateEnum.AWAITING_RESPONSE;
                Instance.AddPendingServiceCallback(callback);
            } else {
                callback.State = ServiceCallbackStateEnum.RESPONSE_SUCCESS;
                callback.ResponseTime = GameTime.fixedTime;
                callback.ResponseDataOfT = callback.State;
            }

            MainThreadManager.Run(() => {
                if (!IsInitialized || Instance.networkObject == null) {
                    return;
                }

                Instance.networkObject.SendRpc(RPC_REGISTER_SCENE, Receivers.Server, pRequireConfirmation, callback.ToByteArray());
            });

            return callback;
        }

        protected virtual void OnRegisterScene_Server (RpcArgs pArgs) {
            bool requireConfirmation = pArgs.GetNext<bool>();
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            RPCNetworkSceneTemplate templateRPC = callbackRPC.data.ByteArrayToObject<RPCNetworkSceneTemplate>();
            NodeNetworkSceneTemplate registeredTemplate = GetRegisteredScene(templateRPC.sceneName);
            bool addSuccess = (registeredTemplate != null && registeredTemplate.NodeId == callbackRPC.sourceNodeId) || AddRegisteredScene(callbackRPC.sourceNodeId, new NodeNetworkSceneTemplate(callbackRPC.sourceNodeId, templateRPC));
            if (requireConfirmation) {
                //Only the callbackRPC.state is important for RPC_CONFIRM
                callbackRPC.state = (addSuccess) ? ServiceCallbackStateEnum.RESPONSE_SUCCESS : ServiceCallbackStateEnum.RESPONSE_FAILED;
                callbackRPC.data = null;
                networkObject.SendRpc(pArgs.Info.SendingPlayer, RPC_CONFIRM_SCENE, callbackRPC.ObjectToByteArray());
            }
        }

        protected virtual void OnRegisterScene_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnRegisterScene_ServerClient (RpcArgs pArgs) {
            // your code here...
        }

        #endregion

        #region UnregisterScene
        public static ServiceCallback<string, ServiceCallbackStateEnum> UnregisterScene (uint pSourceNodeId, bool pRequireConfirmation, string pSceneName) {
            ServiceCallback<string, ServiceCallbackStateEnum> callback = new ServiceCallback<string, ServiceCallbackStateEnum>(0, pSourceNodeId, GameTime.fixedTime);
            if (string.IsNullOrEmpty(pSceneName)) {
                callback.State = ServiceCallbackStateEnum.ERROR_NO_DATA;
                return callback;
            }

            if (!IsInitialized) {
                callback.State = ServiceCallbackStateEnum.ERROR_SERVICE_NOT_INITIALIZED;
                return callback;
            }

            if (Instance.Manager == null || !Instance.Manager.HasNetworker || !Instance.Manager.Networker.IsConnected) {
                callback.State = ServiceCallbackStateEnum.ERROR_NO_CONNECTION;
                return callback;
            }

            callback.RequestDataOfT = pSceneName;
            if (pRequireConfirmation) {
                callback.ResponseDataOfT = ServiceCallbackStateEnum.AWAITING_RESPONSE;
                Instance.AddPendingServiceCallback(callback);
            } else {
                callback.State = ServiceCallbackStateEnum.RESPONSE_SUCCESS;
                callback.ResponseTime = GameTime.fixedTime;
                callback.ResponseDataOfT = callback.State;
            }

            MainThreadManager.Run(() => {
                if (!IsInitialized || Instance.networkObject == null) {
                    return;
                }

                Instance.networkObject.SendRpc(RPC_UNREGISTER_SCENE, Receivers.Server, pRequireConfirmation, callback.ToByteArray());
            });

            return callback;
        }

        protected virtual void OnUnregisterScene_Server (RpcArgs pArgs) {
            bool requireConfirmation = pArgs.GetNext<bool>();
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            string sceneName = callbackRPC.data.ByteArrayToObject<string>();
            bool removeSuccess = RemoveRegisteredScene(callbackRPC.sourceNodeId, sceneName);
            if (requireConfirmation) {
                //Only the callbackRPC.state is important for RPC_CONFIRM
                callbackRPC.state = (removeSuccess) ? ServiceCallbackStateEnum.RESPONSE_SUCCESS : ServiceCallbackStateEnum.RESPONSE_FAILED;
                callbackRPC.data = null;
                networkObject.SendRpc(pArgs.Info.SendingPlayer, RPC_CONFIRM_SCENE, callbackRPC.ObjectToByteArray());
            }
        }

        protected virtual void OnUnregisterScene_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnUnregisterScene_ServerClient (RpcArgs pArgs) {
            // your code here...
        }

        #endregion

        #region ConfirmScene
        protected virtual void OnConfirmScene_Server (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnConfirmScene_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnConfirmScene_ServerClient (RpcArgs pArgs) {
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            ServiceCallback callback;
            if (!_pendingServiceCallbacks.TryGetValue(callbackRPC.callbackId, out callback)) {
                return;
            }

            //RemovePendingServiceCallback(callback.CallbackId);
            callback.State = callbackRPC.state;
            callback.RaiseResponse(GameTime.fixedTime, callbackRPC.state.ObjectToByteArray());
        }

        #endregion

        #region LookupScene
        public static ServiceCallback<string, NodeNetworkSceneTemplate> LookupScene (uint pSourceNodeId, string pSceneName) {
            ServiceCallback<string, NodeNetworkSceneTemplate> callback = new ServiceCallback<string, NodeNetworkSceneTemplate>(0, pSourceNodeId, null, GameTime.fixedTime);
            if (string.IsNullOrEmpty(pSceneName)) {
                callback.State = ServiceCallbackStateEnum.ERROR_NO_DATA;
                return callback;
            }

            if (!IsInitialized) {
                callback.State = ServiceCallbackStateEnum.ERROR_SERVICE_NOT_INITIALIZED;
                return callback;
            }

            if (Instance.Manager == null || !Instance.Manager.HasNetworker || !Instance.Manager.Networker.IsConnected) {
                callback.State = ServiceCallbackStateEnum.ERROR_NO_CONNECTION;
                return callback;
            }

            callback.RequestDataOfT = pSceneName;
            if (Instance.EnableLookupCaching) {
                NodeNetworkSceneTemplate nodeTemplate = Instance.GetCachedLookup(pSceneName);
                if (nodeTemplate != null) {
                    callback.State = ServiceCallbackStateEnum.RESPONSE_SUCCESS;
                    callback.ResponseTime = GameTime.fixedTime;
                    callback.ResponseDataOfT = nodeTemplate;
                    return callback;
                }
            }

            callback.State = ServiceCallbackStateEnum.AWAITING_RESPONSE;
            Instance.AddPendingServiceCallback(callback);
            MainThreadManager.Run(() => {
                if (!IsInitialized || Instance.networkObject == null) {
                    return;
                }

                Instance.networkObject.SendRpc(RPC_LOOKUP_SCENE, Receivers.Server, callback.ToByteArray());
            });

            return callback;
        }

        protected virtual void OnLookupScene_Server (RpcArgs pArgs) {
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            string sceneName = callbackRPC.data.ByteArrayToObject<string>();
            NodeNetworkSceneTemplate nodeTemplate = (GetRegisteredScene(sceneName) ?? new NodeNetworkSceneTemplate(0, -1, String.Empty, RPCVector3.zero, null));
            callbackRPC.data = nodeTemplate.ToByteArray();
            networkObject.SendRpc(pArgs.Info.SendingPlayer, RPC_RECEIVE_LOOKUP_SCENE, nodeTemplate.NodeId, callbackRPC.ObjectToByteArray());
        }

        protected virtual void OnLookupScene_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnLookupScene_ServerClient (RpcArgs pArgs) {
            // your code here...
        }

        #endregion

        #region ReceiveLookupScene
        protected virtual void OnReceiveLookupScene_Server (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnReceiveLookupScene_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnReceiveLookupScene_ServerClient (RpcArgs pArgs) {
            uint nodeId = pArgs.GetNext<uint>();
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            RPCNetworkSceneTemplate templateRPC = callbackRPC.data.ByteArrayToObject<RPCNetworkSceneTemplate>();
            NodeNetworkSceneTemplate nodeTemplate = new NodeNetworkSceneTemplate(nodeId, templateRPC);
            ServiceCallback callback;
            if (!_pendingServiceCallbacks.TryGetValue(callbackRPC.callbackId, out callback)) {
                return;
            }

            if (_enableLookupCaching && nodeTemplate.BuildIndex >= 0) {
                AddCacheLookup(nodeTemplate);
            }

            RemovePendingServiceCallback(callback.CallbackId);
            callback.RaiseResponse(GameTime.fixedTime, nodeTemplate.ObjectToByteArray());
        }

        #endregion

        #region RelayInstantiateInNode
        public static ServiceCallback<RPCInstantiateInNode, ServiceCallbackStateEnum> InstantiateInNode (uint pSourceNodeId, uint pTargetNodeId, string pSceneName, int pCreateCode, IRPCSerializable pBehaviorData, Vector3? pPosition, Quaternion? pRotation, bool pSendTransform) {
            ServiceCallback<RPCInstantiateInNode, ServiceCallbackStateEnum> callback = new ServiceCallback<RPCInstantiateInNode, ServiceCallbackStateEnum>(0, pSourceNodeId, GameTime.fixedTime);
            if (!IsInitialized) {
                callback.State = ServiceCallbackStateEnum.ERROR_SERVICE_NOT_INITIALIZED;
                return callback;
            }

            if (Instance.Manager == null || !Instance.Manager.HasNetworker || !Instance.Manager.Networker.IsConnected) {
                callback.State = ServiceCallbackStateEnum.ERROR_NO_CONNECTION;
                return callback;
            }

            RPCInstantiateInNode requestData = new RPCInstantiateInNode() { targetNodeId = pTargetNodeId, sceneName = pSceneName, createCode = pCreateCode, sendTransform = pSendTransform };
            if (pBehaviorData != null) {
                requestData.behaviorData = pBehaviorData.ToByteArray();
            }

            if (pPosition != null) {
                requestData.position = pPosition.Value.ToRPC();
            }

            if (pRotation != null) {
                requestData.rotation = pRotation.Value.ToRPC();
            }

            callback.RequestDataOfT = requestData;
            callback.State = ServiceCallbackStateEnum.AWAITING_RESPONSE;
            Instance.AddPendingServiceCallback(callback);
            MainThreadManager.Run(() => {
                if (!IsInitialized || Instance.networkObject == null) {
                    return;
                }

                Instance.networkObject.SendRpc(RPC_RELAY_INSTANTIATE_IN_NODE, Receivers.Server, callback.ToByteArray());
            });

            return callback;
        }

        protected virtual void OnRelayInstantiateInNode_Server (RpcArgs pArgs) {
            // We relay the message to the right Node if possible
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            RPCInstantiateInNode instantiateInNodeRPC = callbackRPC.data.ByteArrayToObject<RPCInstantiateInNode>();
            NetworkingPlayer networkingPlayer;
            if (!_nodeToNetworkingPlayer.TryGetValue(instantiateInNodeRPC.targetNodeId, out networkingPlayer)) {
                callbackRPC.state = ServiceCallbackStateEnum.RESPONSE_FAILED;
                callbackRPC.data = null;
                networkObject.SendRpc(pArgs.Info.SendingPlayer, RPC_CONFIRM_INSTANTIATE_IN_NODE, callbackRPC.ObjectToByteArray());
                return;
            }

            networkObject.SendRpc(networkingPlayer, RPC_INSTANTIATE_IN_NODE, data);
        }

        protected virtual void OnRelayInstantiateInNode_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnRelayInstantiateInNode_ServerClient (RpcArgs pArgs) {
            // your code here...
        }

        #endregion

        #region InstantiateInNode
        protected virtual void OnInstantiateInNode_Server (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnInstantiateInNode_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnInstantiateInNode_ServerClient (RpcArgs pArgs) {
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            NetworkBehavior behavior;
            if (NodeManager.IsInitialized) {
                RPCInstantiateInNode instantiateInNodeRPC = callbackRPC.data.ByteArrayToObject<RPCInstantiateInNode>();
                behavior = NodeManager.Instance.InstantiateInScene(instantiateInNodeRPC.sceneName, instantiateInNodeRPC.createCode, null, instantiateInNodeRPC.position.ToVector3(), instantiateInNodeRPC.rotation.ToQuaternion(), instantiateInNodeRPC.sendTransform);
                if (behavior != null) {
                    IRPCSerializable behaviorData = behavior as IRPCSerializable;
                    if (behaviorData != null && instantiateInNodeRPC.behaviorData != null) {
                        behaviorData.FromByteArray(instantiateInNodeRPC.behaviorData);
                    }
                }
            } else {
                behavior = null;
            }

            callbackRPC.state = (behavior != null) ? ServiceCallbackStateEnum.RESPONSE_SUCCESS : ServiceCallbackStateEnum.RESPONSE_FAILED;
            callbackRPC.data = null;
            networkObject.SendRpc(RPC_RELAY_CONFIRM_INSTANTIATE_IN_NODE, Receivers.Server, callbackRPC.ObjectToByteArray());
        }

        #endregion

        #region RelayConfirmInstantiateInNode
        protected virtual void OnRelayConfirmInstantiateInNode_Server (RpcArgs pArgs) {
            // We relay the message to the sourceNodeId
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            NetworkingPlayer networkingPlayer;
            if (!_nodeToNetworkingPlayer.TryGetValue(callbackRPC.sourceNodeId, out networkingPlayer)) {
                return;
            }

            networkObject.SendRpc(networkingPlayer, RPC_CONFIRM_INSTANTIATE_IN_NODE, data);
        }

        protected virtual void OnRelayConfirmInstantiateInNode_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnRelayConfirmInstantiateInNode_ServerClient (RpcArgs pArgs) {
            // your code here...
        }

        #endregion

        #region ConfirmInstantiateInNode
        protected virtual void OnConfirmInstantiateInNode_Server (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnConfirmInstantiateInNode_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnConfirmInstantiateInNode_ServerClient (RpcArgs pArgs) {
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            ServiceCallback callback;
            if (!_pendingServiceCallbacks.TryGetValue(callbackRPC.callbackId, out callback)) {
                return;
            }

            RemovePendingServiceCallback(callback.CallbackId);
            callback.State = callbackRPC.state;
            callback.RaiseResponse(GameTime.fixedTime, callbackRPC.state.ObjectToByteArray());
        }

        #endregion

        #region RelayCreateNetworkSceneInNode
        public static ServiceCallback<RPCCreateNetworkSceneInNode, ServiceCallbackStateEnum> CreateNetworkSceneInNode (uint pSourceNodeId, uint pTargetNodeId, NetworkSceneTemplate pTemplate, bool pAutoAssignIp, bool pAutoAssignPort, byte[] pNetworkSceneMetaData) {
            ServiceCallback<RPCCreateNetworkSceneInNode, ServiceCallbackStateEnum> callback = new ServiceCallback<RPCCreateNetworkSceneInNode, ServiceCallbackStateEnum>(0, pSourceNodeId, GameTime.fixedTime);
            if (pTemplate == null) {
                callback.State = ServiceCallbackStateEnum.ERROR_NO_DATA;
                return callback;
            }

            if (!IsInitialized) {
                callback.State = ServiceCallbackStateEnum.ERROR_SERVICE_NOT_INITIALIZED;
                return callback;
            }

            if (Instance.Manager == null || !Instance.Manager.HasNetworker || !Instance.Manager.Networker.IsConnected) {
                callback.State = ServiceCallbackStateEnum.ERROR_NO_CONNECTION;
                return callback;
            }

            RPCCreateNetworkSceneInNode requestData = new RPCCreateNetworkSceneInNode() { targetNodeId = pTargetNodeId, template = pTemplate.ToRPC(), autoAssignIp = pAutoAssignIp, autoAssignPort = pAutoAssignPort, networkSceneMetaData = pNetworkSceneMetaData };
            callback.RequestDataOfT = requestData;
            callback.State = ServiceCallbackStateEnum.AWAITING_RESPONSE;
            Instance.AddPendingServiceCallback(callback);
            MainThreadManager.Run(() => {
                if (!IsInitialized || Instance.networkObject == null) {
                    return;
                }

                Instance.networkObject.SendRpc(RPC_RELAY_CREATE_NETWORK_SCENE_IN_NODE, Receivers.Server, callback.ToByteArray());
            });

            return callback;
        }

        protected virtual void OnRelayCreateNetworkSceneInNode_Server (RpcArgs pArgs) {
            // We relay the message to the right Node if possible
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            RPCCreateNetworkSceneInNode createNetworkSceneInNodeRPC = callbackRPC.data.ByteArrayToObject<RPCCreateNetworkSceneInNode>();
            NetworkingPlayer networkingPlayer;
            if (!_nodeToNetworkingPlayer.TryGetValue(createNetworkSceneInNodeRPC.targetNodeId, out networkingPlayer)) {
                callbackRPC.state = ServiceCallbackStateEnum.RESPONSE_FAILED;
                callbackRPC.data = null;
                networkObject.SendRpc(pArgs.Info.SendingPlayer, RPC_CONFIRM_CREATE_NETWORK_SCENE_IN_NODE, callbackRPC.ObjectToByteArray());
                return;
            }

            networkObject.SendRpc(networkingPlayer, RPC_CREATE_NETWORK_SCENE_IN_NODE, data);
        }

        protected virtual void OnRelayCreateNetworkSceneInNode_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnRelayCreateNetworkSceneInNode_ServerClient (RpcArgs pArgs) {
            // your code here...
        }

        #endregion

        #region InstantiateInNode
        protected virtual void OnCreateNetworkSceneInNode_Server (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnCreateNetworkSceneInNode_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnCreateNetworkSceneInNode_ServerClient (RpcArgs pArgs) {
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            NetworkSceneItem item;
            if (NodeManager.IsInitialized) {
                RPCCreateNetworkSceneInNode createNetworkSceneInNodeRPC = callbackRPC.data.ByteArrayToObject<RPCCreateNetworkSceneInNode>();
                NetworkSceneTemplate template = new NetworkSceneTemplate(createNetworkSceneInNodeRPC.template);
                if (createNetworkSceneInNodeRPC.autoAssignIp) {
                    template.Settings.ServerAddress.Ip = NodeManager.Instance.MasterManager.Settings.ServerAddress.Ip;
                    template.Settings.ClientAddress.Ip = NodeManager.Instance.MasterManager.Settings.ClientAddress.Ip;
                }

                if (createNetworkSceneInNodeRPC.autoAssignPort) {
                    template.Settings.ServerAddress.Port = NodeManager.Instance.UsedDynamicPorts.PeekNext();
                    template.Settings.ClientAddress.Port = template.Settings.ServerAddress.Port;
                }

                item = NodeManager.Instance.CreateNetworkScene(template, false, createNetworkSceneInNodeRPC.networkSceneMetaData);
            } else {
                item = null;
            }

            callbackRPC.state = (item != null) ? ServiceCallbackStateEnum.RESPONSE_SUCCESS : ServiceCallbackStateEnum.RESPONSE_FAILED;
            callbackRPC.data = null;
            networkObject.SendRpc(RPC_RELAY_CONFIRM_CREATE_NETWORK_SCENE_IN_NODE, Receivers.Server, callbackRPC.ObjectToByteArray());
        }

        #endregion

        #region RelayConfirmCreateNetworkSceneInNode
        protected virtual void OnRelayConfirmCreateNetworkSceneInNode_Server (RpcArgs pArgs) {
            // We relay the message to the sourceNodeId
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            NetworkingPlayer networkingPlayer;
            if (!_nodeToNetworkingPlayer.TryGetValue(callbackRPC.sourceNodeId, out networkingPlayer)) {
                return;
            }

            networkObject.SendRpc(networkingPlayer, RPC_CONFIRM_CREATE_NETWORK_SCENE_IN_NODE, data);
        }

        protected virtual void OnRelayConfirmCreateNetworkSceneInNode_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnRelayConfirmCreateNetworkSceneInNode_ServerClient (RpcArgs pArgs) {
            // your code here...
        }

        #endregion

        #region ConfirmInstantiateInNode
        protected virtual void OnConfirmCreateNetworkSceneInNode_Server (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnConfirmCreateNetworkSceneInNode_Client (RpcArgs pArgs) {
            // your code here...
        }

        protected virtual void OnConfirmCreateNetworkSceneInNode_ServerClient (RpcArgs pArgs) {
            byte[] data = pArgs.GetNext<byte[]>();
            RPCServiceCallback callbackRPC = data.ByteArrayToObject<RPCServiceCallback>();
            ServiceCallback callback;
            if (!_pendingServiceCallbacks.TryGetValue(callbackRPC.callbackId, out callback)) {
                return;
            }

            RemovePendingServiceCallback(callback.CallbackId);
            callback.State = callbackRPC.state;
            callback.RaiseResponse(GameTime.fixedTime, callbackRPC.state.ObjectToByteArray());
        }

        #endregion

        #region Helpers
        public virtual void AddPendingServiceCallback (ServiceCallback pCallback) {
            if (pCallback == null) {
                return;
            }

            pCallback.CallbackId = _usedServiceCallbackIds.GetNext();
            _pendingServiceCallbacks.Add(pCallback.CallbackId, pCallback);
        }

        public virtual void RemovePendingServiceCallback (uint pCallbackId) {
            _usedServiceCallbackIds.Free(pCallbackId);
            _pendingServiceCallbacks.Remove(pCallbackId);
        }

        public virtual bool AddRegisteredScene (uint pNodeId, NodeNetworkSceneTemplate pNodeTemplate) {
            if (pNodeTemplate == null) {
                return false;
            }

            Dictionary<string, NodeNetworkSceneTemplate> templates;
            if (!_scenesRegistered.TryGetValue(pNodeId, out templates)) {
                templates = new Dictionary<string, NodeNetworkSceneTemplate>();
                _scenesRegistered.Add(pNodeId, templates);
            }

            if (templates.ContainsKey(pNodeTemplate.SceneName)) {
                return false;
            }

            templates.Add(pNodeTemplate.SceneName, pNodeTemplate);
            return true;
        }

        public virtual bool RemoveRegisteredScene (uint pNodeId, string pSceneName) {
            Dictionary<string, NodeNetworkSceneTemplate> templates;
            if (!_scenesRegistered.TryGetValue(pNodeId, out templates)) {
                return false;
            }

            return templates.Remove(pSceneName);
        }

        public virtual bool AddCacheLookup (NodeNetworkSceneTemplate pNodeTemplate) {
            if (pNodeTemplate == null) {
                return false;
            }
        
            return _scenesCached.Add(pNodeTemplate.SceneName, pNodeTemplate);
        }

        public virtual void RemoveCacheLookup (string pSceneName) {
            _scenesCached.Remove(pSceneName);
        }

        public virtual NodeNetworkSceneTemplate GetCachedLookup (string pSceneName) {
            return _scenesCached.Get(pSceneName);
        }

        protected virtual NodeNetworkSceneTemplate GetRegisteredScene (string pSceneName) {
            NodeNetworkSceneTemplate nodeTemplate;
            foreach (var node in _scenesRegistered.Values) {
                if (node.TryGetValue(pSceneName, out nodeTemplate)) {
                    return nodeTemplate;
                }
            }

            return null;
        }

        public virtual void SetNetworkObject (NetworkObject pNetworkObject) {
            networkObject = (NodeServiceNetworkObject)pNetworkObject;
        }

        public virtual NetworkObject GetNetworkObject () {
            return networkObject;
        }

        public uint GetNetworkId () {
            return networkObject.NetworkId;
        }

        #endregion

        #region Events
        protected virtual void PendingServiceCallbacks_OnCacheItemExpired (uint pKey, CacheItem<ServiceCallback, float> pItem) {
            RemovePendingServiceCallback(pKey);
            pItem.Value.State = ServiceCallbackStateEnum.ERROR_TIMEOUT;
            pItem.Value.RaiseTimeout();
        }

        protected virtual void ScenesCached_OnCacheItemExpired (string pKey, CacheItem<NodeNetworkSceneTemplate, float> pItem) {
            _scenesCached.Remove(pKey);
        }

        protected virtual void PlayerDisconnected_Server (NetworkingPlayer pPlayer, NetWorker pSender) {
            uint? nodeIdToRemove = null;
            foreach (var item in _nodeToNetworkingPlayer) {
                if (item.Value.NetworkId == pPlayer.NetworkId) {
                    nodeIdToRemove = item.Key;
                    break;
                }
            }

            if (nodeIdToRemove == null) {
                return;
            }

            _scenesRegistered.Remove(nodeIdToRemove.Value);
            _nodeToNetworkingPlayer.Remove(nodeIdToRemove.Value);
        }

        protected virtual void PlayerDisconnected_Client (NetWorker pSender) {
            networkObject.Destroy();
        }

        #endregion

        #region RPC-Callbacks
        public override void RegisterNode (RpcArgs pArgs) {
            if (OnRegisterNode != null) {
                OnRegisterNode(pArgs);
            }
        }

        public override void RegisterScene (RpcArgs pArgs) {
            if (OnRegisterScene != null) {
                OnRegisterScene(pArgs);
            }
        }

        public override void UnregisterScene (RpcArgs pArgs) {
            if (OnUnregisterScene != null) {
                OnUnregisterScene(pArgs);
            }
        }

        public override void ConfirmScene (RpcArgs pArgs) {
            if (OnConfirmScene != null) {
                OnConfirmScene(pArgs);
            }
        }

        public override void LookupScene (RpcArgs pArgs) {
            if (OnLookupScene != null) {
                OnLookupScene(pArgs);
            }
        }

        public override void ReceiveLookupScene (RpcArgs pArgs) {
            if (OnReceiveLookupScene != null) {
                OnReceiveLookupScene(pArgs);
            }
        }

        public override void RelayInstantiateInNode (RpcArgs pArgs) {
            if (OnRelayInstantiateInNode != null) {
                OnRelayInstantiateInNode(pArgs);
            }
        }

        public override void InstantiateInNode (RpcArgs pArgs) {
            if(OnInstantiateInNode != null) {
                OnInstantiateInNode(pArgs);
            }
        }

        public override void RelayConfirmInstantiateInNode (RpcArgs pArgs) {
            if (OnRelayConfirmInstantiateInNode != null) {
                OnRelayConfirmInstantiateInNode(pArgs);
            }
        }

        public override void ConfirmInstantiateInNode (RpcArgs pArgs) {
            if (OnConfirmInstantiateInNode != null) {
                OnConfirmInstantiateInNode(pArgs);
            }
        }

        public override void RelayCreateNetworkSceneInNode (RpcArgs pArgs) {
            if (OnRelayCreateNetworkSceneInNode != null) {
                OnRelayCreateNetworkSceneInNode(pArgs);
            }
        }

        public override void CreateNetworkSceneInNode (RpcArgs pArgs) {
            if (OnCreateNetworkSceneInNode != null) {
                OnCreateNetworkSceneInNode(pArgs);
            }
        }

        public override void RelayConfirmCreateNetworkSceneInNode (RpcArgs pArgs) {
            if (OnRelayConfirmCreateNetworkSceneInNode != null) {
                OnRelayConfirmCreateNetworkSceneInNode(pArgs);
            }
        }

        public override void ConfirmCreateNetworkSceneInNode (RpcArgs pArgs) {
            if (OnConfirmCreateNetworkSceneInNode != null) {
                OnConfirmCreateNetworkSceneInNode(pArgs);
            }
        }

        #endregion
    }
}

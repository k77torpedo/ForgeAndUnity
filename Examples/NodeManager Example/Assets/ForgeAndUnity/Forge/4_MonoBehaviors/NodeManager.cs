using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;
using ForgeAndUnity.Unity;

/*-----------------------------+-------------------------------\
|                                                              |
|                       !!WARNING!!                            |
|                                                              |
|  These libraries are under heavy development and are         |
|  subject to many changes as development continues.           |
|  Using these libraries in any test- or productive            |
|  environment is at your own discretion!                      |
|                                                              |
\------------------------------+------------------------------*/

namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Creates and manages <see cref="NetworkSceneManager"/>s. 
    /// Provides communication with other <see cref="NodeManager"/>s.
    /// </summary>
    public class NodeManager : MonoBehaviour {
        //Fields
        public const string                                     MASTER_SCENE_NAME                           = "MasterScene";
        public const float                                      CACHE_LIFETIME_PENDING_SCENES               = 30f;
        public static NodeManager                               Instance;
        public static bool                                      IsInitialized                               { get { return Instance != null; } }

        [SerializeField] protected GameObject                   _networkSceneManagerPrefab;
        [SerializeField] protected bool                         _autoReconnectMasterManager;
        [SerializeField] protected float                        _autoReconnectMasterManagerInterval;
        [SerializeField] protected bool                         _enableRegisterDynamicScenes;
        [SerializeField] protected bool                         _registerDynamicScenesRequireConfirmation;
        [SerializeField] protected bool                         _enableSceneLookupCaching;
        [SerializeField] protected NodeMapSO                    _nodeMapSO;
        [SerializeField] protected NetworkBehaviorListSO        _serviceNetworkSceneBehaviorListSO;
        [SerializeField] protected NetworkBehaviorListSO        _networkSceneBehaviorListSO;
        protected Dictionary<string, NetworkSceneItem>          _scenesStatic;
        protected Dictionary<string, NetworkSceneItem>          _scenesDynamic;
        protected Node                                          _currentNode;
        protected Node                                          _masterNode;
        protected NetworkSceneManager                           _masterManager;
        protected HandlerPoolUShort                             _usedDynamicPorts;
        protected CacheList<NetworkSceneItem, float>            _pendingScenes;
        protected bool                                          _isServer;
        protected IEnumerator                                   _updatePendingScenes;

        public NodeMapSO                                        NodeMapSO                                   { get { return _nodeMapSO; } }
        public NetworkBehaviorListSO                            ServiceNetworkSceneBehaviorListSO           { get { return _serviceNetworkSceneBehaviorListSO; } }
        public NetworkBehaviorListSO                            NetworkSceneBehaviorListSO                  { get { return _networkSceneBehaviorListSO; } }
        public HandlerPoolUShort                                UsedDynamicPorts                            { get { return _usedDynamicPorts; } }
        public CacheList<NetworkSceneItem, float>               PendingScenes                               { get { return _pendingScenes; } }
        public Node                                             CurrentNode                                 { get { return _currentNode; } }
        public Node                                             MasterNode                                  { get { return _masterNode; } }
        public Dictionary<string, NetworkSceneItem>             ScenesStatic                                { get { return _scenesStatic; } }
        public Dictionary<string, NetworkSceneItem>             ScenesDynamic                               { get { return _scenesDynamic; } }
        public NetworkSceneManager                              MasterManager                               { get { return _masterManager; } }
        public bool                                             IsServer                                    { get { return _isServer; } }
        public bool                                             EnableRegisterDynamicScenes                 { get { return _enableRegisterDynamicScenes; } }
        public bool                                             RegisterDynamicScenesRequireConfirmaton     { get { return _registerDynamicScenesRequireConfirmation; } }
        public bool                                             EnableLookupCaching                         { get { return _enableSceneLookupCaching; } }
        public bool                                             HasMasterManager                            { get { return _masterManager != null; } }
        public bool                                             IsMasterNode                                { get { return (_currentNode != null && _masterNode != null && _currentNode.NodeId == _masterNode.NodeId && _currentNode.IsMasterNode); } }

        //Events
        public delegate void MasterManagerFailedToBindEvent ();
        public event MasterManagerFailedToBindEvent OnMasterManagerFailedToBind;
        public delegate void PendingSceneTimeoutEvent (int pIndex, NetworkSceneItem pItem);
        public event PendingSceneTimeoutEvent OnPendingSceneTimeout;
        public delegate void PlayerChangingNetworkSceneEvent (NetworkSceneTemplate pTemplate);
        public event PlayerChangingNetworkSceneEvent OnPlayerChangingNetworkScene;
        public delegate void PlayerChangingNetworkSceneCompletedEvent (NetworkSceneItem pItem);
        public event PlayerChangingNetworkSceneCompletedEvent OnPlayerChangingNetworkSceneCompleted;
        public delegate void PlayerChangingNetworkSceneFailedEvent (NetworkSceneItem pItem);
        public event PlayerChangingNetworkSceneFailedEvent OnPlayerChangingNetworkSceneFailed;


        //Functions
        #region Unity
        protected virtual void Awake () {
            if (Instance != null) {
                Destroy(gameObject);
                return;
            }
        
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _scenesStatic = new Dictionary<string, NetworkSceneItem>();
            _scenesDynamic = new Dictionary<string, NetworkSceneItem>();
            _usedDynamicPorts = new HandlerPoolUShort();
            _pendingScenes = new CacheList<NetworkSceneItem, float>(new DelayFixedTime(GameTime.FixedTimeUpdater()), CACHE_LIFETIME_PENDING_SCENES);
            _pendingScenes.OnCacheItemExpired += PendingScenes_OnCacheItemExpired;
            _updatePendingScenes = _pendingScenes.UpdateCoroutine();
            if (_nodeMapSO != null) {
                _nodeMapSO.nodeMap.Init();
            }

            if (_serviceNetworkSceneBehaviorListSO != null) {
                _serviceNetworkSceneBehaviorListSO.behaviorList.Init();
            }

            if (_networkSceneBehaviorListSO != null) {
                _networkSceneBehaviorListSO.behaviorList.Init();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        protected virtual void Update () {
            _updatePendingScenes.MoveNext();
        }

        protected virtual void OnDestroy () {
            UnloadNetworkScenes(true, true);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Disconnect();
        }

        protected virtual void OnApplicationQuit () {
            Disconnect();
            NetWorker.EndSession();
        }

        #endregion

        #region Start
        public virtual void StartAsServer (uint pNodeId) {
            StartAsServer(_nodeMapSO.nodeMap.GetByNodeId(pNodeId));
        }

        public virtual void StartAsServer (string pNodeName) {
            StartAsServer(_nodeMapSO.nodeMap.GetByNodeName(pNodeName));
        }

        public virtual void StartAsServer (Node pNode) {
            _isServer = true;
            _currentNode = pNode;
            _masterNode = _nodeMapSO.nodeMap.GetMasterNode();
            Disconnect();
            if (_masterNode != null) {
                if (IsMasterNode) {
                    _masterManager = CreateMasterNetworkScene(_currentNode, _serviceNetworkSceneBehaviorListSO);
                    if (_masterManager != null) {
                        _masterManager.AutoReconnect = _autoReconnectMasterManager;
                        _masterManager.AutoReconnectInterval = _autoReconnectMasterManagerInterval;
                        _masterManager.StartAsServer();
                        InstantiateServices();
                    }
                } else {
                    _masterManager = CreateMasterNetworkScene(_masterNode, _serviceNetworkSceneBehaviorListSO);
                    if (_masterManager != null) {
                        _masterManager.AutoReconnect = _autoReconnectMasterManager;
                        _masterManager.AutoReconnectInterval = _autoReconnectMasterManagerInterval;
                        _masterManager.StartAsClient();
                    }
                }

                if (_masterManager == null || !_masterManager.HasNetworker || !_masterManager.Networker.IsBound) {
                    RaiseMasterManagerFailedToBind();
                }
            }

            _usedDynamicPorts.UseFreeIds = false;
            _usedDynamicPorts.LowerBound = _currentNode.DynamicPortMin;
            _usedDynamicPorts.UpperBound = _currentNode.DynamicPortMax;
            _usedDynamicPorts.NextIdentifier = _currentNode.DynamicPortMin;
            CreateNetworkScenes(_currentNode.InitWithNetworkScenes, true);
        }

        public virtual void StartAsClient (string pStartSceneName) {
            StartAsClient(_nodeMapSO.nodeMap.GetTemplateBySceneName(pStartSceneName));
        }

        public virtual void StartAsClient (NetworkSceneTemplate pTemplate) {
            _isServer = false;
            CreateNetworkScene(pTemplate, true);
        }

        #endregion

        #region Create NetworkBehaviors
        public virtual NetworkBehavior InstantiateInScene (string pSceneName, string pNetworkBehaviorName, IRPCSerializable pBehaviorData = null, Vector3? pPosition = null, Quaternion? pRotation = null, bool pSendTransform = true) {
            return InstantiateInScene(pSceneName, _networkSceneBehaviorListSO.behaviorList.GetCreateCodeFromName(pNetworkBehaviorName), pBehaviorData, pPosition, pRotation, pSendTransform);
        }

        public virtual NetworkBehavior InstantiateInScene (string pSceneName, int pCreateCode = -1, IRPCSerializable pBehaviorData = null, Vector3? pPosition = null, Quaternion? pRotation = null, bool pSendTransform = true) {
            NetworkSceneItem item = FindNetworkSceneItem(pSceneName, true, true);
            if (item == null || !item.HasManager) {
                return null;
            }

            return item.Manager.InstantiateNetworkBehavior(pCreateCode, pBehaviorData, pPosition, pRotation, pSendTransform);
        }

        public virtual ServiceCallback<RPCInstantiateInNode, ServiceCallbackStateEnum> InstantiateInNode (uint pTargetNodeId, string pSceneName, int pCreateCode, IRPCSerializable pBehaviorData = null, Vector3? pPosition = null, Quaternion? pRotation = null, bool pSendTransform = true) {
            return InstantiateInNode(_currentNode.NodeId, pTargetNodeId, pSceneName, pCreateCode, pBehaviorData, pPosition, pRotation, pSendTransform);
        }

        public virtual ServiceCallback<RPCInstantiateInNode, ServiceCallbackStateEnum> InstantiateInNode (uint pTargetNodeId, string pSceneName, string pNetworkBehaviorName, IRPCSerializable pBehaviorData = null, Vector3? pPosition = null, Quaternion? pRotation = null, bool pSendTransform = true) {
            return InstantiateInNode(_currentNode.NodeId, pTargetNodeId, pSceneName, _networkSceneBehaviorListSO.behaviorList.GetCreateCodeFromName(pNetworkBehaviorName), pBehaviorData, pPosition, pRotation, pSendTransform);
        }

        public virtual ServiceCallback<RPCInstantiateInNode, ServiceCallbackStateEnum> InstantiateInNode (uint pSourceNodeId, uint pTargetNodeId, string pSceneName, string pNetworkBehaviorName, IRPCSerializable pBehaviorData = null, Vector3? pPosition = null, Quaternion? pRotation = null, bool pSendTransform = true) {
            return InstantiateInNode(pSourceNodeId, pTargetNodeId, pSceneName, _networkSceneBehaviorListSO.behaviorList.GetCreateCodeFromName(pNetworkBehaviorName), pBehaviorData, pPosition, pRotation, pSendTransform);
        }

        public virtual ServiceCallback<RPCInstantiateInNode, ServiceCallbackStateEnum> InstantiateInNode (uint pSourceNodeId, uint pTargetNodeId, string pSceneName, int pCreateCode = -1, IRPCSerializable pBehaviorData = null, Vector3? pPosition = null, Quaternion? pRotation = null, bool pSendTransform = true) {
            return NodeService.InstantiateInNode(pSourceNodeId, pTargetNodeId, pSceneName, pCreateCode, pBehaviorData, pPosition, pRotation, pSendTransform);
        }

        #endregion

        #region Create NetworkScenes
        public virtual NetworkSceneItem[] CreateNetworkScenes (NetworkSceneTemplate[] pSceneTemplates, bool pIsSceneStatic, byte[] pNetworkSceneMetaData = null) {
            List<NetworkSceneItem> items = new List<NetworkSceneItem>();
            for (int i = 0; i < pSceneTemplates.Length; i++) {
                NetworkSceneItem item = CreateNetworkScene(pSceneTemplates[i], pIsSceneStatic, pNetworkSceneMetaData);
                if (item == null) {
                    continue;
                }

                items.Add(item);
            }

            return items.ToArray();
        }

        public virtual NetworkSceneItem CreateNetworkScene (NetworkSceneTemplate pSceneTemplate, byte[] pNetworkSceneMetaData = null) {
            return CreateNetworkScene(pSceneTemplate, IsStaticScene(pSceneTemplate.SceneName), pNetworkSceneMetaData);
        }

        public virtual NetworkSceneItem CreateNetworkScene (NetworkSceneTemplate pSceneTemplate, bool pIsSceneStatic, byte[] pNetworkSceneMetaData = null) {
            ushort port = GetPortFromSetting(pSceneTemplate.Settings);
            if (!pIsSceneStatic && _usedDynamicPorts.Contains(port)) {
                return null;
            }

            // Check if we already have created a scene with that name
            NetworkSceneTemplate existingScene = FindNetworkSceneTemplate(pSceneTemplate.SceneName, false, true, true, true);
            if (existingScene != null) {
                return null;
            }

            // Create an empty Scene and put a new NetworkSceneManager in it
            NetworkSceneManager manager = CreateEmptyNetworkScene(pSceneTemplate);
            manager.NetworkSceneMetaData = pNetworkSceneMetaData;

            // Create the Scene with the 'BuildIndex' and queue it up to be merged with our empty Scene
            NetworkSceneItem newItem = new NetworkSceneItem(pSceneTemplate, manager);
            if (pSceneTemplate.BuildIndex > 0) {
                SceneManager.LoadScene(pSceneTemplate.BuildIndex, LoadSceneMode.Additive);
                _pendingScenes.Add(newItem);
            } else {
                newItem.IsCreated = true;
            }

            // Add the newItem to the respective collection
            if (pIsSceneStatic) {
                _scenesStatic.Add(pSceneTemplate.SceneName, newItem);
            } else {
                _usedDynamicPorts.Add(port);
                _scenesDynamic.Add(pSceneTemplate.SceneName, newItem);
            }

            // If the scene is 'dynamic' we need to register it so other Nodes know the scene is taken
            if (_isServer && !pIsSceneStatic && _enableRegisterDynamicScenes) {
                RegisterDynamicScene(_registerDynamicScenesRequireConfirmation, pSceneTemplate);
                newItem.IsRegistered = !_registerDynamicScenesRequireConfirmation;
            } else {
                newItem.IsRegistered = true;
            }

            ReadyNetworkScene(newItem);
            return newItem;
        }

        public virtual ServiceCallback<RPCCreateNetworkSceneInNode, ServiceCallbackStateEnum> CreateNetworkSceneInNode (uint pTargetNodeId, NetworkSceneTemplate pTemplate, bool pAutoAssignIp = false, bool pAutoAssignPort = false, byte[] pNetworkSceneMetaData = null) {
            return NodeService.CreateNetworkSceneInNode(_currentNode.NodeId, pTargetNodeId, pTemplate, pAutoAssignIp, pAutoAssignPort, pNetworkSceneMetaData);
        }

        public virtual ServiceCallback<RPCCreateNetworkSceneInNode, ServiceCallbackStateEnum> CreateNetworkSceneInNode (uint pSourceNodeId, uint pTargetNodeId, NetworkSceneTemplate pTemplate, bool pAutoAssignIp = false, bool pAutoAssignPort = false, byte[] pNetworkSceneMetaData = null) {
            return NodeService.CreateNetworkSceneInNode(pSourceNodeId, pTargetNodeId, pTemplate, pAutoAssignIp, pAutoAssignPort, pNetworkSceneMetaData);
        }

        public virtual NetworkSceneManager CreateEmptyNetworkScene (NetworkSceneTemplate pTemplate) {
            return CreateEmptyNetworkScene(pTemplate, _networkSceneBehaviorListSO);
        }

        public virtual NetworkSceneManager CreateEmptyNetworkScene (NetworkSceneTemplate pTemplate, NetworkBehaviorListSO pBehaviorListSO) {
            Scene newScene = SceneManager.CreateScene(pTemplate.SceneName);
            GameObject go = GameObject.Instantiate(_networkSceneManagerPrefab);
            go.name = newScene.name + "_NetworkSceneManager";
            NetworkSceneManager manager = go.GetComponent<NetworkSceneManager>();
            manager.NetworkBehaviorListSO = pBehaviorListSO;
            manager.Settings = pTemplate.Settings;
            SceneManager.MoveGameObjectToScene(go, newScene);
            return manager;
        }

        protected virtual NetworkSceneManager CreateMasterNetworkScene (Node pNode, NetworkBehaviorListSO pBehaviorListSO) {
            if (pNode == null) {
                return null;
            }

            return CreateEmptyNetworkScene(new NetworkSceneTemplate(0, MASTER_SCENE_NAME, RPCVector3.zero, pNode.MasterNodeSetting), pBehaviorListSO);
        }

        public virtual void UnloadNetworkScenes (bool pUnloadScenesStatic, bool pUnloadScenesDynamic) {
            if (pUnloadScenesStatic) {
                foreach (var item in _scenesStatic.Values) {
                    UnloadNetworkScene(item);
                }
            }

            if (pUnloadScenesDynamic) {
                foreach (var item in _scenesDynamic.Values) {
                    UnloadNetworkScene(item);
                }
            }
        }

        public virtual void UnloadNetworkScene (string pSceneName) {
            UnloadNetworkScene(FindNetworkSceneItem(pSceneName, true, true));
        }

        public virtual void UnloadNetworkScene (NetworkSceneItem pItem) {
            if (pItem == null || !pItem.HasManager) {
                return;
            }

            if (pItem.IsCreated) {
                SceneManager.UnloadSceneAsync(pItem.Manager.gameObject.scene.name);
            } else {
                //The scene is still being created. If we mark it as "IsUnregistered" it will be deleted right after it has been instantiated.
                pItem.IsUnregistered = true;
            }
        }

        public virtual void ReadyNetworkScene (NetworkSceneItem pItem) {
            if (!pItem.IsCreated || !pItem.IsRegistered || pItem.IsUnregistered || pItem.IsReady) {
                return;
            }

            if (_isServer) {
                pItem.Manager.StartAsServer();
            } else {
                pItem.Manager.StartAsClient();
            }

            pItem.IsReady = true;
            pItem.RaiseReady();
        }

        #endregion

        #region Scene-Registration and -Lookup
        public virtual ServiceCallback<RPCNetworkSceneTemplate, ServiceCallbackStateEnum> RegisterDynamicScene (NetworkSceneTemplate pTemplate) {
            return RegisterDynamicScene(_registerDynamicScenesRequireConfirmation, pTemplate);
        }

        public virtual ServiceCallback<RPCNetworkSceneTemplate, ServiceCallbackStateEnum> RegisterDynamicScene (bool pRequireConfirmation, NetworkSceneTemplate pTemplate) {
            return RegisterDynamicScene(_currentNode.NodeId, pRequireConfirmation, pTemplate);
        }

        public virtual ServiceCallback<RPCNetworkSceneTemplate, ServiceCallbackStateEnum> RegisterDynamicScene (uint pSourceNodeId, bool pRequireConfirmation, NetworkSceneTemplate pTemplate) {
            ServiceCallback<RPCNetworkSceneTemplate, ServiceCallbackStateEnum> callback = NodeService.RegisterScene(pSourceNodeId, pRequireConfirmation, pTemplate);
            if (callback.State == ServiceCallbackStateEnum.AWAITING_RESPONSE) {
                callback.OnResponse += (pResponseTime, pResponseData, pSender) => {
                    ConfirmRegisterDynamicScene(callback);
                };
            }

            return callback;
        }

        public virtual ServiceCallback<string, ServiceCallbackStateEnum> UnregisterDynamicScene (string pSceneName) {
            return UnregisterDynamicScene(false, pSceneName);
        }

        public virtual ServiceCallback<string, ServiceCallbackStateEnum> UnregisterDynamicScene (bool pRequireConfirmation, string pSceneName) {
            return UnregisterDynamicScene(_currentNode.NodeId, pRequireConfirmation, pSceneName);
        }

        public virtual ServiceCallback<string, ServiceCallbackStateEnum> UnregisterDynamicScene (uint pSourceNodeId, bool pRequireConfirmation, string pSceneName) {
            return NodeService.UnregisterScene(pSourceNodeId, pRequireConfirmation, pSceneName);
        }

        public virtual ServiceCallback<string, NodeNetworkSceneTemplate> LookUpNetworkSceneTemplate (string pSceneName) {
            return LookUpNetworkSceneTemplate(_currentNode.NodeId, pSceneName);
        }

        public virtual ServiceCallback<string, NodeNetworkSceneTemplate> LookUpNetworkSceneTemplate (uint pSourceNodeId, string pSceneName) {
            return NodeService.LookupScene(pSourceNodeId, pSceneName);
        }

        protected virtual void ConfirmRegisterDynamicScene (ServiceCallback<RPCNetworkSceneTemplate, ServiceCallbackStateEnum> pCallback) {
            NetworkSceneItem item = FindNetworkSceneItemInDynamicScenes(pCallback.RequestDataOfT.sceneName);
            if (item == null) {
                return;
            }

            if (pCallback.ResponseDataOfT == ServiceCallbackStateEnum.RESPONSE_SUCCESS) {
                item.IsRegistered = true;
                item.RaiseRegistered();
                ReadyNetworkScene(item);
            } else {
                item.IsUnregistered = true;
                item.RaiseUnregistered();
                UnloadNetworkScene(item);
            }
        }

        #endregion

        #region Helpers
        public virtual NetworkSceneItem FindNetworkSceneItemInStaticScenes (string pSceneName) {
            NetworkSceneItem item;
            if (_scenesStatic.TryGetValue(pSceneName, out item)) {
                return item;
            }

            return null;
        }

        public virtual NetworkSceneItem FindNetworkSceneItemInDynamicScenes (string pSceneName) {
            NetworkSceneItem item;
            if (_scenesDynamic.TryGetValue(pSceneName, out item)) {
                return item;
            }

            return null;
        }

        public virtual NetworkSceneItem FindNetworkSceneItem (string pSceneName, bool pSearchScenesStatic, bool pSearchScenesDynamic) {
            //check our static scenes
            if (pSearchScenesStatic) {
                NetworkSceneItem item = FindNetworkSceneItemInStaticScenes(pSceneName);
                if (item != null) {
                    return item;
                }
            }

            //check our dynamic scenes
            if (pSearchScenesDynamic) {
                NetworkSceneItem item = FindNetworkSceneItemInDynamicScenes(pSceneName);
                if (item != null) {
                    return item;
                }
            }

            return null;
        }

        public virtual NetworkSceneTemplate FindNetworkSceneTemplateInNodeMap (string pSceneName) {
            return _nodeMapSO.nodeMap.GetTemplateBySceneName(pSceneName);
        }

        public virtual NetworkSceneTemplate FindNetworkSceneTemplateInStaticScenes (string pSceneName) {
            NetworkSceneItem item;
            if (_scenesStatic.TryGetValue(pSceneName, out item)) {
                return item.SceneTemplate;
            }

            return null;
        }

        public virtual NetworkSceneTemplate FindNetworkSceneTemplateInDynamicScenes (string pSceneName) {
            NetworkSceneItem item;
            if (_scenesDynamic.TryGetValue(pSceneName, out item)) {
                return item.SceneTemplate;
            }

            return null;
        }

        public virtual NodeNetworkSceneTemplate FindNetworkSceneTemplateInCachedScenes (string pSceneName) {
            if (!NodeService.IsInitialized) {
                return null;
            }

            return NodeService.Instance.GetCachedLookup(pSceneName);
        }

        public virtual NetworkSceneTemplate FindNetworkSceneTemplate (string pSceneName, bool pSearchNodeMap, bool pSearchStaticScenes, bool pSearchDynamicScenes, bool pSearchCachedScenes) {
            //check in our static NodeMap
            if (pSearchNodeMap) {
                NetworkSceneTemplate template = FindNetworkSceneTemplateInNodeMap(pSceneName);
                if (template != null) {
                    return template;
                }
            }

            //check our static scenes
            if (pSearchStaticScenes) {
                NetworkSceneTemplate template = FindNetworkSceneTemplateInStaticScenes(pSceneName);
                if (template != null) {
                    return template;
                }
            }

            //check our dynamic scenes
            if (pSearchDynamicScenes) {
                NetworkSceneTemplate template = FindNetworkSceneTemplateInDynamicScenes(pSceneName);
                if (template != null) {
                    return template;
                }
            }

            //check in our cached scenes
            if (pSearchCachedScenes) {
                NetworkSceneTemplate template = FindNetworkSceneTemplateInCachedScenes(pSceneName);
                if (template != null) {
                    return template;
                }
            }

            return null;
        }

        public virtual bool IsStaticScene (string pSceneName) {
            // All Scenes that are in our NodeMap are always considered 'static'
            return (FindNetworkSceneTemplateInNodeMap(pSceneName) != null);
        }

        public virtual bool SceneExists (string pSceneName) {
            return (FindNetworkSceneTemplate(pSceneName, false, true, true, true) != null);
        }

        public virtual bool TryGetNetworkSceneManager (string pSceneName, out NetworkSceneManager pNetworkSceneManager) {
            NetworkSceneItem item = FindNetworkSceneItem(pSceneName, true, true);
            if (item == null || !item.HasManager) {
                pNetworkSceneManager = null;
                return false;
            }

            pNetworkSceneManager = item.Manager;
            return true;
        }

        public virtual NetworkSceneManager FindNetworkSceneManager (string pSceneName) {
            NetworkSceneItem item = FindNetworkSceneItem(pSceneName, true, true);
            if (item == null || !item.HasManager) {
                return null;
            }

            return item.Manager;
        }

        public virtual ushort GetPortFromSetting (NetworkSceneManagerSetting pSetting) {
            return (_isServer) ? pSetting.ServerAddress.Port : pSetting.ClientAddress.Port;
        }

        protected virtual void ApplySceneOffset (Scene pScene, Vector3 pSceneOffset) {
            GameObject[] rootGameObjects = pScene.GetRootGameObjects();
            for (int i = 0; i < rootGameObjects.Length; i++) {
                rootGameObjects[i].transform.position = rootGameObjects[i].transform.position + pSceneOffset;
            }
        }

        protected virtual void InstantiateServices () {
            if (_masterManager == null || _serviceNetworkSceneBehaviorListSO == null) {
                return;
            }

            foreach (var service in _serviceNetworkSceneBehaviorListSO.behaviorList.NetworkBehaviors.Values) {
                _masterManager.InstantiateNetworkBehavior(service.CreateCode, null, null, null, false);
            }
        }

        #endregion

        #region Disconnect
        public virtual void Disconnect () {
            if (HasMasterManager) {
                SceneManager.UnloadSceneAsync(_masterManager.gameObject.scene.name);
            }
        }

        #endregion

        #region Events
        protected virtual void RaiseMasterManagerFailedToBind () {
            if (OnMasterManagerFailedToBind != null) {
                OnMasterManagerFailedToBind();
            }
        }

        protected virtual void RaisePendingSceneTimeout (int pIndex, NetworkSceneItem pItem) {
            if (OnPendingSceneTimeout != null) {
                OnPendingSceneTimeout(pIndex, pItem);
            }
        }

        public virtual void RaisePlayerChangingNetworkScene (NetworkSceneTemplate pTemplate) {
            if (OnPlayerChangingNetworkScene != null) {
                OnPlayerChangingNetworkScene(pTemplate);
            }
        }

        public virtual void RaisePlayerChangingNetworkSceneCompleted (NetworkSceneItem pItem) {
            if (OnPlayerChangingNetworkSceneCompleted != null) {
                OnPlayerChangingNetworkSceneCompleted(pItem);
            }
        }

        public virtual void RaisePlayerChangingNetworkSceneFailed (NetworkSceneItem pItem) {
            if (OnPlayerChangingNetworkSceneFailed != null) {
                OnPlayerChangingNetworkSceneFailed(pItem);
            }
        }

        public virtual void PlayerChangingSceneSucceeded (NetworkSceneItem item) {
            item.OnReady -= PlayerChangingSceneSucceeded;
            item.OnUnloaded -= PlayerChangingSceneFailed;
            RaisePlayerChangingNetworkSceneCompleted(item);
        }

        public virtual void PlayerChangingSceneFailed (NetworkSceneItem item = null) {
            if (item != null) {
                item.OnReady -= PlayerChangingSceneSucceeded;
                item.OnUnloaded -= PlayerChangingSceneFailed;
            }

            RaisePlayerChangingNetworkSceneFailed(item);
        }

        protected virtual void PendingScenes_OnCacheItemExpired (int pIndex, CacheItem<NetworkSceneItem, float> pItem) {
            Debug.Log("Expired!");
            RaisePendingSceneTimeout(pIndex, pItem.Value);
        }

        protected virtual void OnSceneLoaded (Scene pScene, LoadSceneMode pSceneMode) {
            NetworkSceneItem item;
            for (int i = 0; i < _pendingScenes.CacheItems.Count; i++) {
                if (_pendingScenes.CacheItems[i].Value.SceneTemplate.BuildIndex == pScene.buildIndex) {
                    item = _pendingScenes.CacheItems[i].Value;
                    ApplySceneOffset(pScene, item.SceneTemplate.SceneOffset.ToVector3());
                    SceneManager.MergeScenes(pScene, item.Manager.gameObject.scene);
                    item.IsCreated = true;
                    item.RaiseCreated();
                    if (item.IsUnregistered) {
                        UnloadNetworkScene(item);
                    } else {
                        ReadyNetworkScene(item);
                    }

                    _pendingScenes.RemoveAt(i);
                    return;
                }
            }
        }

        protected virtual void OnSceneUnloaded (Scene pScene) {
            NetworkSceneItem item = FindNetworkSceneItemInStaticScenes(pScene.name);
            bool isStaticScene = (item != null);
            if (!isStaticScene) {
                item = FindNetworkSceneItemInDynamicScenes(pScene.name);
            }

            if (item == null) {
                return;
            }

            if (_isServer && !isStaticScene && _enableRegisterDynamicScenes) {
                UnregisterDynamicScene(item.SceneTemplate.SceneName);
            }

            item.RaiseUnloaded();
            if (isStaticScene) {
                _scenesStatic.Remove(pScene.name);
            } else {
                _usedDynamicPorts.Free(GetPortFromSetting(item.SceneTemplate.Settings));
                _scenesDynamic.Remove(pScene.name);
            }
        }

        #endregion
    }
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeardedManStudios;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking.Frame;
using BeardedManStudios.Forge.Networking.Unity;

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

/// <summary>
/// A <see cref="NetworkManager"/> dedicated to one <see cref="Scene"/>.
/// </summary>
public class NetworkSceneManager : MonoBehaviour {
    //Fields
    [SerializeField] protected NetworkBehaviorListSO            _networkBehaviorListSO;
    [SerializeField] protected bool                             _autoReconnect;
    [SerializeField] protected float                            _autoReconnectInterval;
    protected DelayFixedTime                                    _autoReconnectDelay;
    protected NetworkSceneManagerSetting                        _settings;
    protected Dictionary<int, INetworkBehavior>                 _pendingObjects;
    protected Dictionary<int, NetworkObject>                    _pendingNetworkObjects;
    protected NetWorker                                         _networker;
    protected byte[]                                            _networkSceneMetaData;
    protected BMSByte                                           _tmpMetadata;

    public NetworkBehaviorListSO            NetworkBehaviorListSO           { get { return _networkBehaviorListSO; } set { _networkBehaviorListSO = value; } }
    public bool                             AutoReconnect                   { get { return _autoReconnect; } set { _autoReconnect = value; } }
    public float                            AutoReconnectInterval           { get { return _autoReconnectInterval; } set { _autoReconnectInterval = value; } }
    public DelayFixedTime                   AutoReconnectDelay              { get { return _autoReconnectDelay; } set { _autoReconnectDelay = value; } }
    public NetworkSceneManagerSetting       Settings                        { get { return _settings; } set { _settings = value; } }
    public NetWorker                        Networker                       { get { return _networker; } protected set { _networker = value; } }
    public byte[]                           NetworkSceneMetaData            { get { return _networkSceneMetaData; } set { _networkSceneMetaData = value; } }
    public bool                             HasNetworker                    { get { return _networker != null; } }
    public bool                             IsServer                        { get { return (_networker is IServer); } }
    
    //Events
    public delegate void InstantiateEvent (INetworkBehavior pUnityGameObject, NetworkObject pObj);
    public event InstantiateEvent OnObjectInitialized;
    public delegate void NetworkSceneReadyEvent (NetworkSceneManager pNetworkSceneManager, Scene pScene);
    public event NetworkSceneReadyEvent OnNetworkSceneStart;
    public delegate void NetworkSceneFailedToBindEvent (NetworkSceneManager pNetworkSceneManager, Scene pScene);
    public event NetworkSceneFailedToBindEvent OnNetworkSceneFailedToBind;
    public delegate void AutoReconnectAttemptEvent ();
    public event AutoReconnectAttemptEvent OnAutoReconnectAttempt;
    public delegate void AutoReconnectEvent ();
    public event AutoReconnectEvent OnAutoReconnect;


    //Functions
    #region Unity
    protected virtual void Awake () {
        _autoReconnectDelay = new DelayFixedTime(GameTime.FixedTimeUpdater());
        _autoReconnectDelay.Stop();
        _settings = new NetworkSceneManagerSetting();
        _pendingObjects = new Dictionary<int, INetworkBehavior>();
        _pendingNetworkObjects = new Dictionary<int, NetworkObject>();
        _tmpMetadata = new BMSByte();
        if (_networkBehaviorListSO != null) {
            _networkBehaviorListSO.behaviorList.Init();
        }
    }

    protected virtual void FixedUpdate () {
        if (_networker != null) {
            for (int i = 0; i < _networker.NetworkObjectList.Count; i++)
                _networker.NetworkObjectList[i].InterpolateUpdate();
        }
    }

    protected virtual void LateUpdate () {
        if (_autoReconnect) {
            CheckAutoReconnect();
        }
    }

    protected virtual void OnDestroy () {
        if (_networker != null) {
            if (_networker.IsServer) {
                UnregisterEventsServer();
            } else {
                UnregisterEventsClient();
            }
        }

        Disconnect();
    }

    protected virtual void OnApplicationQuit () {
        Disconnect();
        NetWorker.EndSession();
    }

    #endregion

    #region Start
    public virtual void StartAsServer () {
        StartAsServer(gameObject.scene, _settings);
    }

    public virtual void StartAsServer (Scene pScene, NetworkSceneManagerSetting pSettings) {
        _settings = pSettings;
        SceneManager.MoveGameObjectToScene(gameObject, pScene);
        Disconnect();
        InitializeDefaults();
        NetWorker server;
        if (_settings.UseTCP) {
            server = new TCPServer(_settings.MaxConnections);
            ((TCPServer)server).Connect(_settings.ServerAddress.Ip, _settings.ServerAddress.Port);
        } else {
            server = new UDPServer(_settings.MaxConnections);
            if (_settings.ServerNATAddress.Ip.Trim().Length == 0) {
                ((UDPServer)server).Connect(_settings.ServerAddress.Ip, _settings.ServerAddress.Port);
            } else {
                ((UDPServer)server).Connect(_settings.ServerAddress.Ip, _settings.ServerAddress.Port, _settings.ServerNATAddress.Ip, _settings.ServerNATAddress.Port);
            }
        }

        if (!server.IsBound) {
            RaiseNetworkSceneFailedToBind(this, pScene);
            return;
        }

        UnregisterEventsServer();
        _networker = server;
        RegisterEventsServer();
        SceneReady(pScene);
        RaiseNetworkSceneStart(this, pScene);
    }

    public virtual void StartAsClient () {
        StartAsClient(gameObject.scene, _settings);
    }

    public virtual void StartAsClient (Scene pScene, NetworkSceneManagerSetting pSettings) {
        _settings = pSettings;
        SceneManager.MoveGameObjectToScene(gameObject, pScene);
        Disconnect();
        InitializeDefaults();
        NetWorker client;
        if (_settings.UseTCP) {
            client = new TCPClient();
            ((TCPClient)client).Connect(_settings.ClientAddress.Ip, _settings.ClientAddress.Port);
        } else {
            client = new UDPClient();
            if (_settings.ClientNATAddress.Ip.Trim().Length == 0) {
                ((UDPClient)client).Connect(_settings.ClientAddress.Ip, _settings.ClientAddress.Port);
            } else {
                ((UDPClient)client).Connect(_settings.ClientAddress.Ip, _settings.ClientAddress.Port, _settings.ClientNATAddress.Ip, _settings.ClientNATAddress.Port);
            }
        }

        if (!client.IsBound) {
            RaiseNetworkSceneFailedToBind(this, gameObject.scene);
            return;
        }

        UnregisterEventsClient();
        _networker = client;
        RegisterEventsClient();
        SceneReady(gameObject.scene);
        SceneManager.MoveGameObjectToScene(gameObject, gameObject.scene);
        RaiseNetworkSceneStart(this, gameObject.scene);
    }

    #endregion

    #region Disconnect
    public virtual void Disconnect () {
        _pendingObjects.Clear();
        _pendingNetworkObjects.Clear();
        if (_networker != null) {
            _networker.Disconnect(false);
            NetworkObject.ClearNetworkObjects(_networker);
            NetworkObject.Flush(_networker);
        }
    }

    #endregion

    #region Register Events
    protected virtual void RegisterEventsClient () {
        if (_networker != null) {
            _networker.objectCreated += CreatePendingObjects;
            _networker.objectCreated += CaptureObjects;
            _networker.binaryMessageReceived += ReadBinaryClient;
        }

        OnObjectInitialized += MoveObjectToScene;
    }

    protected virtual void UnregisterEventsClient () {
        if (_networker != null) {
            _networker.objectCreated -= CreatePendingObjects;
            _networker.objectCreated -= CaptureObjects;
            _networker.binaryMessageReceived -= ReadBinaryClient;
        }
        
        OnObjectInitialized -= MoveObjectToScene;
    }

    protected virtual void RegisterEventsServer () {
        if (_networker != null) {
            _networker.objectCreated += CreatePendingObjects;
            _networker.objectCreated += CaptureObjects;
            _networker.binaryMessageReceived += ReadBinaryServer;
        }

        OnObjectInitialized += MoveObjectToScene;
    }

    protected virtual void UnregisterEventsServer () {
        if (_networker != null) {
            _networker.objectCreated -= CreatePendingObjects;
            _networker.objectCreated -= CaptureObjects;
            _networker.binaryMessageReceived -= ReadBinaryServer;
        }

        OnObjectInitialized -= MoveObjectToScene;
    }

    #endregion

    #region InstantiateNetworkBehavior
    public virtual NetworkBehavior InstantiateNetworkBehavior (string pName, IRPCSerializable pBehaviorData = null, Vector3? pPosition = null, Quaternion? pRotation = null, bool pSendTransform = true) {
        return InstantiateNetworkBehavior(_networkBehaviorListSO.behaviorList.GetCreateCodeFromName(pName), pBehaviorData, pPosition, pRotation, pSendTransform);
    }

    public virtual NetworkBehavior InstantiateNetworkBehavior (int pCreateCode = -1, IRPCSerializable pBehaviorData = null, Vector3? pPosition = null, Quaternion? pRotation = null, bool pSendTransform = true) {
        GameObject prefab = _networkBehaviorListSO.behaviorList.GetByCreateCode(pCreateCode);
        if (prefab == null) {
            return null;
        }

        var go = Instantiate(prefab);
        var netBehavior = go.GetComponent<NetworkBehavior>();
        NetworkObject obj = null;
        if (!pSendTransform && pPosition == null && pRotation == null) {
            obj = netBehavior.CreateNetworkObject(_networker, pCreateCode);
        } else {
            _tmpMetadata.Clear();
            if (pPosition == null && pRotation == null) {
                byte transformFlags = 0x1 | 0x2;
                ObjectMapper.Instance.MapBytes(_tmpMetadata, transformFlags);
                ObjectMapper.Instance.MapBytes(_tmpMetadata, go.transform.position, go.transform.rotation);
            } else {
                byte transformFlags = 0x0;
                transformFlags |= (byte)(pPosition != null ? 0x1 : 0x0);
                transformFlags |= (byte)(pRotation != null ? 0x2 : 0x0);
                ObjectMapper.Instance.MapBytes(_tmpMetadata, transformFlags);

                if (pPosition != null)
                    ObjectMapper.Instance.MapBytes(_tmpMetadata, pPosition.Value);

                if (pRotation != null)
                    ObjectMapper.Instance.MapBytes(_tmpMetadata, pRotation.Value);
            }

            obj = netBehavior.CreateNetworkObject(_networker, pCreateCode, _tmpMetadata.CompressBytes());
        }

        SetINetworkSceneObject(netBehavior, obj);
        SetIRPCSerializable(netBehavior, pBehaviorData);
        FinalizeInitialization(go, netBehavior, obj, pPosition, pRotation, pSendTransform);
        return netBehavior;
    }

    #endregion

    #region BinaryMessages
    public virtual bool ChangePlayerNetworkScene (NetworkSceneTemplate pTemplate, NetworkingPlayer pPlayer) {
        if (pTemplate == null) {
            return false;
        }

        RPCPlayerChangeNetworkScene changeSceneRPC = RPCPlayerChangeNetworkScene.FromNetworkSceneTemplate(pTemplate);
        Binary sceneTemplateFrame = new Binary(_networker.Time.Timestep, false, changeSceneRPC.ObjectToByteArray(), Receivers.Target, CustomMessageGroupIds.PLAYER_CHANGE_NETWORK_SCENE, pPlayer.Networker is BaseTCP);
        SendFrame(sceneTemplateFrame, pPlayer);
        return true;
    }

    protected virtual void ReadBinaryServer (NetworkingPlayer pPlayer, Binary pFrame, NetWorker pSender) { }

    protected virtual void ReadBinaryClient (NetworkingPlayer pPlayer, Binary pFrame, NetWorker pSender) {
        if (pFrame.GroupId == CustomMessageGroupIds.PLAYER_CHANGE_NETWORK_SCENE) {
            byte[] data = pFrame.StreamData.byteArr;
            RPCPlayerChangeNetworkScene changeSceneRPC = data.ByteArrayToObject<RPCPlayerChangeNetworkScene>();
            NetworkSceneTemplate sceneTemplate = RPCPlayerChangeNetworkScene.ToNetworkSceneTemplate(changeSceneRPC);
            MainThreadManager.Run(() => {
                if (NodeManager.IsInitialized) {
                    NodeManager.Instance.UnloadNetworkScenes(true, true);
                    NodeManager.Instance.RaisePlayerChangingNetworkScene(sceneTemplate);
                    NetworkSceneItem item = NodeManager.Instance.CreateNetworkScene(sceneTemplate, true);
                    if (item == null) {
                        NodeManager.Instance.PlayerChangingSceneFailed(item);
                        return;
                    }

                    if (item.IsReady) {
                        NodeManager.Instance.PlayerChangingSceneSucceeded(item);
                    } else {
                        item.OnReady += NodeManager.Instance.PlayerChangingSceneSucceeded;
                        item.OnUnloaded += NodeManager.Instance.PlayerChangingSceneFailed;
                    }
                }
            });

            return;
        }
    }

    #endregion

    #region Helpers
    public virtual void InitializeDefaults () {
        MainThreadManager.Create();
        if (!_settings.UseTCP) {
            NetWorker.PingForFirewall(_settings.ServerAddress.Port);
        }

        if (_settings.UseMainThreadManagerForRPCs && Rpc.MainThreadRunner == null) {
            Rpc.MainThreadRunner = MainThreadManager.Instance;
        }

        UnityObjectMapper.Instance.UseAsDefault();
        if (NetworkObject.Factory == null) {
            NetworkObject.Factory = new NetworkObjectFactory();
        }
    }

    protected virtual void CheckAutoReconnect () {
        if (!HasNetworker || _autoReconnectInterval <= 0f) {
            return;
        }

        if (_networker.IsBound) {
            if (!_autoReconnectDelay.HasStopped) {
                _autoReconnectDelay.Stop();
            }
            
            return;
        }

        if (!_autoReconnectDelay.HasStopped) {
            if (!_autoReconnectDelay.HasPassed) {
                return;
            }

            RaiseAutoReconnectAttempt();
            if (_networker.IsServer) {
                StartAsServer();
            } else {
                StartAsClient();
            }

            if (_networker.IsBound) {
                _autoReconnectDelay.Stop();
                RaiseAutoReconnect();
            } else {
                _autoReconnectDelay.Start(_autoReconnectInterval);
            }
        } else {
            _autoReconnectDelay.Start(_autoReconnectInterval);
        }
    }

    public virtual void SendFrame (FrameStream pFrame, NetworkingPlayer pTargetPlayer = null) {
        if (_networker is IServer) {
            if (pTargetPlayer != null) {
                if (_networker is TCPServer) {
                    ((TCPServer)_networker).SendToPlayer(pFrame, pTargetPlayer);
                } else {
                    ((UDPServer)_networker).Send(pTargetPlayer, pFrame, true);
                }
            } else {
                if (_networker is TCPServer) {
                    ((TCPServer)_networker).SendAll(pFrame);
                } else {
                    ((UDPServer)_networker).Send(pFrame, true);
                }
            }
        } else {
            if (_networker is TCPClientBase) {
                ((TCPClientBase)_networker).Send(pFrame);
            } else {
                ((UDPClient)_networker).Send(pFrame, true);
            }
        }
    }

    public virtual void SceneReady (Scene pScene) {
        //When a scene is loaded we clear our NetWorker and search for any NetWorkBehaviors that have been inside the Scene and initialize them on the Network.
        _pendingObjects.Clear();
        _pendingNetworkObjects.Clear();
        int currentAttachCode = 1;

        //We only need to lookup NetworkBehaviors in our own scene
        GameObject[] rootGameObjects = pScene.GetRootGameObjects();
        var behaviors = new List<NetworkBehavior>();
        var behaviorsInitialized = new List<NetworkBehavior>();
        for (int i = 0; i < rootGameObjects.Length; i++) {
            NetworkBehavior[] behaviorsFound = rootGameObjects[i].GetComponentsInChildren<NetworkBehavior>();
            behaviors.AddRange(behaviorsFound.Where(b => !b.Initialized));
            behaviorsInitialized.AddRange(behaviorsFound.Where(b => b.Initialized));
        }

        if (_networker is IClient) {
            if (behaviorsInitialized.Count > 0) {
                foreach (var behavior in behaviorsInitialized) {
                    if (behavior.TempAttachCode > 0) {
                        continue;
                    }

                    Destroy(behavior.gameObject);
                }
            }

            if (behaviors.Count == 0 && behaviorsInitialized.Count == 0) {
                NetworkObject.Flush(_networker);
                if (_pendingObjects.Count == 0) {
                    _networker.objectCreated -= CreatePendingObjects;
                }

                return;
            }
        }

        foreach (NetworkBehavior behavior in behaviors) {
            behavior.TempAttachCode = pScene.buildIndex << 16;
            behavior.TempAttachCode += currentAttachCode++;
            behavior.TempAttachCode = -behavior.TempAttachCode;
        }

        if (_networker is IClient) {
            foreach (NetworkBehavior behavior in behaviors) {
                _pendingObjects.Add(behavior.TempAttachCode, behavior);
            }

            foreach (var behavior in behaviorsInitialized) {
                if (behavior.TempAttachCode == 0) {
                    continue;
                }

                NetworkObject obj = behavior.CreateNetworkObject(_networker, behavior.TempAttachCode);
                _pendingObjects.Add(behavior.TempAttachCode, behavior);
            }

            NetworkObject.Flush(_networker, null, CreatePendingObjects);
            if (_pendingObjects.Count == 0) {
                _networker.objectCreated -= CreatePendingObjects;
            }

        } else {
            foreach (INetworkBehavior behavior in behaviors) {
                SetINetworkSceneObject(behavior);
                behavior.Initialize(_networker);
            }

            foreach (NetworkBehavior behavior in behaviorsInitialized) {
                NetworkObject obj = behavior.CreateNetworkObject(_networker, behavior.TempAttachCode);
                SetINetworkSceneObject(behavior, obj);
                behavior.Initialize(obj);
                FinalizeInitialization(behavior.gameObject, behavior, obj, behavior.gameObject.transform.position, behavior.gameObject.transform.rotation, true);
            }
        }
    }

    protected virtual void FinalizeInitialization (GameObject pGo, INetworkBehavior pNetBehavior, NetworkObject pObj, Vector3? pPosition = null, Quaternion? pRotation = null, bool pSendTransform = true, bool pSkipOthers = false) {
        if (IsServer) {
            InitializedObject(pNetBehavior, pObj);
        } else {
            pObj.pendingInitialized += InitializedObject;
        }

        if (pPosition != null) {
            if (pRotation != null) {
                pGo.transform.position = pPosition.Value;
                pGo.transform.rotation = pRotation.Value;
            } else {
                pGo.transform.position = pPosition.Value;
            }
        }

        //testvariables : test this function and comment it in if applicable
        //if (sendTransform)
        // obj.SendRpc(NetworkBehavior.RPC_SETUP_TRANSFORM, Receivers.AllBuffered, go.transform.position, go.transform.rotation);

        if (!pSkipOthers) {
            // Go through all associated network behaviors in the hierarchy (including self) and
            // Assign their TempAttachCode for lookup later. Should use an incrementor or something
            uint idOffset = 1;
            ProcessOthers(pGo.transform, pObj, ref idOffset, (NetworkBehavior)pNetBehavior);
        }
    }

    protected virtual void ProcessOthers (Transform pObj, NetworkObject pCreateTarget, ref uint pIdOffset, NetworkBehavior pNetBehavior = null) {
        int i;

        // Get the order of the components as they are in the inspector
        var components = pObj.GetComponents<NetworkBehavior>();

        // Create each network object that is available
        for (i = 0; i < components.Length; i++) {
            if (components[i] == pNetBehavior) {
                continue;
            }

            var no = components[i].CreateNetworkObject(_networker, 0);

            if (_networker.IsServer) {
                FinalizeInitialization(pObj.gameObject, components[i], no, pObj.position, pObj.rotation, false, true);
            } else {
                components[i].AwaitNetworkBind(_networker, pCreateTarget, pIdOffset++);
            }
        }

        for (i = 0; i < pObj.transform.childCount; i++) {
            ProcessOthers(pObj.transform.GetChild(i), pCreateTarget, ref pIdOffset);
        }
    }

    public virtual void SetINetworkSceneObject (INetworkBehavior pNetworkBehavior, NetworkObject pNetworkObject = null) {
        if (pNetworkBehavior == null) {
            return;
        }

        INetworkSceneObject nObj = pNetworkBehavior as INetworkSceneObject;
        if (nObj == null) {
            return;
        }

        nObj.Manager = this;
        if (pNetworkObject != null) {
            nObj.SetNetworkObject(pNetworkObject);
        }
    }

    public virtual void SetIRPCSerializable (INetworkBehavior pNetworkBehavior, IRPCSerializable pBehaviorData) {
        if (pNetworkBehavior == null || pBehaviorData == null) {
            return;
        }

        IRPCSerializable rpcSerializable = pNetworkBehavior as IRPCSerializable;
        if (rpcSerializable == null) {
            return;
        }

        rpcSerializable.FromByteArray(pBehaviorData.ToByteArray());
    }

    #endregion

    #region Events
    protected virtual void RaiseNetworkSceneFailedToBind(NetworkSceneManager pNetworkSceneManager, Scene pScene) {
        if (OnNetworkSceneFailedToBind != null) {
            OnNetworkSceneFailedToBind(pNetworkSceneManager, pScene);
        }
    }

    protected virtual void RaiseNetworkSceneStart (NetworkSceneManager pNetworkSceneManager, Scene pScene) {
        if (OnNetworkSceneStart != null) {
            OnNetworkSceneStart(pNetworkSceneManager, pScene);
        }
    }

    protected virtual void RaiseAutoReconnectAttempt () {
        if (OnAutoReconnectAttempt != null) {
            OnAutoReconnectAttempt();
        }
    }

    protected virtual void RaiseAutoReconnect () {
        if (OnAutoReconnect != null) {
            OnAutoReconnect();
        }
    }

    protected virtual void MoveObjectToScene (INetworkBehavior pUnityGameObject, NetworkObject pObj) {
        NetworkBehavior behavior = pUnityGameObject as NetworkBehavior;
        if (behavior == null) {
            return;
        }

        MainThreadManager.Run(() => {
            if (behavior == null || gameObject == null) {
                return;
            }

            SceneManager.MoveGameObjectToScene(behavior.gameObject, gameObject.scene);
        });
    }

    protected virtual void CaptureObjects (NetworkObject pObj) {
        if (pObj.CreateCode < 0) {
            return;
        }

        MainThreadManager.Run(() => {
            NetworkBehavior newObj = null;
            if (!NetworkBehavior.skipAttachIds.TryGetValue(pObj.NetworkId, out newObj)) {
                GameObject templateObj = _networkBehaviorListSO.behaviorList.GetByCreateCode(pObj.CreateCode);
                if (templateObj != null) {
                    var go = Instantiate(templateObj);
                    newObj = go.GetComponent<NetworkBehavior>();
                }
            }

            if (newObj == null) {
                return;
            }

            SetINetworkSceneObject(newObj, pObj);
            newObj.Initialize(pObj);
            if (OnObjectInitialized != null) {
                OnObjectInitialized(newObj, pObj);
            }
        });
    }

    protected virtual void CreatePendingObjects (NetworkObject pObj) {
        INetworkBehavior behavior;
        if (!_pendingObjects.TryGetValue(pObj.CreateCode, out behavior)) {
            if (pObj.CreateCode < 0) {
                _pendingNetworkObjects.Add(pObj.CreateCode, pObj);
            }

            return;
        }

        SetINetworkSceneObject(behavior, pObj);
        behavior.Initialize(pObj);
        _pendingObjects.Remove(pObj.CreateCode);
        if (_pendingObjects.Count == 0) {
            _networker.objectCreated -= CreatePendingObjects;
        }
    }

    protected virtual void InitializedObject (INetworkBehavior pBehavior, NetworkObject pObj) {
        if (OnObjectInitialized != null) {
            OnObjectInitialized(pBehavior, pObj);
        }

        pObj.pendingInitialized -= InitializedObject;
    }

    #endregion
}

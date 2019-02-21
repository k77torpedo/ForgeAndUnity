using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;

/// <summary>
/// Moves a <see cref="NetworkBehavior"/> that implements the <see cref="INetworkSceneObject"/>-interface to another 'NetworkScene'.
/// </summary>
public class NetworkSceneTeleporter : MonoBehaviour {
    //Fields
    [SerializeField] protected string                               _sceneName;
    [SerializeField] protected Transform                            _teleportTarget;
    [SerializeField] protected bool                                 _enableLookup;
    [SerializeField] protected float                                _findSceneInterval;
    protected NodeNetworkSceneTemplate                              _nodeTemplate;
    protected Dictionary<uint, NetworkBehavior>                     _pendingObjects;
    protected DelayFixedTime                                        _findSceneDelay;
    protected ServiceCallback<string, NodeNetworkSceneTemplate>     _currentLookup;

    public string                                           SceneName               { get { return _sceneName; } set { _sceneName = value; } }
    public Transform                                        TeleportTarget          { get { return _teleportTarget; } set { _teleportTarget = value; } }
    public bool                                             EnableLookup            { get { return _enableLookup; } set { _enableLookup = value; } }
    public float                                            FindSceneInterval       { get { return _findSceneInterval; } set { _findSceneInterval = value; } }
    public NodeNetworkSceneTemplate                         NodeTemplate            { get { return _nodeTemplate; } set { _nodeTemplate = value; } }
    public Dictionary<uint, NetworkBehavior>                PendingObjects          { get { return _pendingObjects; } }


    //Functions
    #region Unity
    protected virtual void Awake () {
        _pendingObjects = new Dictionary<uint, NetworkBehavior>();
        _findSceneDelay = new DelayFixedTime(GameTime.FixedTimeUpdater());
        if (_nodeTemplate == null) {
            FindNetworkSceneTemplate();
        }
    }

    protected virtual void OnTriggerEnter (Collider pOther) {
        if (!NodeManager.IsInitialized || !NodeManager.Instance.IsServer) {
            return;
        }

        AddPendingObject(pOther.gameObject);
        if (_nodeTemplate == null) {
            FindNetworkSceneTemplate();
        } else {
            TeleportPendingObjects();
        }
    }

    #endregion

    #region Helpers
    public virtual void FindNetworkSceneTemplate () {
        if (_nodeTemplate != null || !NodeManager.IsInitialized || !NodeManager.Instance.IsServer || _currentLookup != null || !_findSceneDelay.HasPassed) {
            return;
        }

        // Lookup if the scene resides on our Node
        if (_nodeTemplate == null) {
            NetworkSceneTemplate foundTemplate = NodeManager.Instance.FindNetworkSceneTemplate(_sceneName, false, true, true, false);
            if (foundTemplate != null) {
                _nodeTemplate = new NodeNetworkSceneTemplate(NodeManager.Instance.CurrentNode.NodeId, foundTemplate);
                return;
            }
        }

        // Lookup the NodeMap if there is a static NetworkScene we can find on another Node
        if (_nodeTemplate == null && NodeManager.Instance.NodeMapSO != null) {
            _nodeTemplate = NodeManager.Instance.NodeMapSO.nodeMap.GetNodeTemplateBySceneName(_sceneName);
            if (_nodeTemplate != null) {
                return;
            }
        }

        // Lookup if the scene is a dynamic NetworkScene on another Node
        if (_nodeTemplate == null && _enableLookup && _currentLookup == null) {
            _currentLookup = NodeManager.Instance.LookUpNetworkSceneTemplate(_sceneName);
            if (_currentLookup.State == ServiceCallbackStateEnum.RESPONSE_SUCCESS) {
                _nodeTemplate = _currentLookup.ResponseDataOfT;
                _currentLookup = null;
            } else if (_currentLookup.State == ServiceCallbackStateEnum.AWAITING_RESPONSE) {
                _currentLookup.OnResponseOfT += CurrentLookup_OnResponseOfT;
                _currentLookup.OnTimeout += CurrentLookup_OnTimeout;
            } else {
                _pendingObjects.Clear();
                _currentLookup = null;
            }
        }

        _findSceneDelay.Start(_findSceneInterval);
    }

    public virtual void AddPendingObject (GameObject pGameObject) {
        NetworkBehavior behavior = pGameObject.GetComponent<NetworkBehavior>();
        if (behavior == null) {
            return;
        }

        INetworkSceneObject networkSceneObject = behavior as INetworkSceneObject;
        uint networkId = networkSceneObject.GetNetworkId();
        if (networkSceneObject == null || _pendingObjects.ContainsKey(networkId)) {
            return;
        }

        _pendingObjects.Add(networkId, behavior);
    }

    protected virtual void TeleportPendingObjects () {
        if (_nodeTemplate == null || !NodeManager.IsInitialized || !NodeManager.Instance.IsServer) {
            return;
        }

        foreach (var item in _pendingObjects.Values) {
            InstantiatePendingObject(item);
        }

        _pendingObjects.Clear();
    }

    protected virtual void InstantiatePendingObject(NetworkBehavior pBehavior) {
        if (pBehavior == null) {
            return;
        }

        INetworkSceneObject networkSceneObject = pBehavior as INetworkSceneObject;
        NetworkObject nObj = networkSceneObject.GetNetworkObject();
        if (nObj == null) {
            return;
        }

        if (_nodeTemplate.NodeId == NodeManager.Instance.CurrentNode.NodeId) {
            NetworkBehavior behavior = NodeManager.Instance.InstantiateInScene(_nodeTemplate.SceneName, nObj.CreateCode, (pBehavior as IRPCSerializable), _teleportTarget.position, _teleportTarget.rotation);
            if (behavior == null) {
                return;
            }

            DestroyPendingObject(pBehavior);
        } else {
            ServiceCallback<RPCInstantiateInNode, ServiceCallbackStateEnum> callback = NodeManager.Instance.InstantiateInNode(_nodeTemplate.NodeId, _nodeTemplate.SceneName, nObj.CreateCode, (pBehavior as IRPCSerializable), _teleportTarget.position, _teleportTarget.rotation);
            BMSLogger.Instance.Log(callback.State.ToString());
            if (callback.State == ServiceCallbackStateEnum.AWAITING_RESPONSE) {
                callback.OnResponseOfT += (pResponseTime, pResponseDataOfT, pSender) => {
                    Callback_InstantiateInNode(callback, pBehavior);
                };
            }
        }
    }

    protected virtual void DestroyPendingObject (NetworkBehavior pBehavior) {
        GameObject.Destroy(pBehavior.gameObject);
    }
    #endregion

    #region Events
    protected virtual void CurrentLookup_OnResponseOfT (float pResponseTime, NodeNetworkSceneTemplate pResponseDataOfT, ServiceCallback<NodeNetworkSceneTemplate> pSender) {
        _currentLookup.OnResponseOfT -= CurrentLookup_OnResponseOfT;
        _currentLookup.OnTimeout -= CurrentLookup_OnTimeout;
        _currentLookup = null;
        _nodeTemplate = pResponseDataOfT;
    }

    protected virtual void CurrentLookup_OnTimeout (ServiceCallback pSender) {
        _currentLookup.OnResponseOfT -= CurrentLookup_OnResponseOfT;
        _currentLookup.OnTimeout -= CurrentLookup_OnTimeout;
        _currentLookup = null;
        _pendingObjects.Clear();
    }

    protected virtual void Callback_InstantiateInNode (ServiceCallback<RPCInstantiateInNode, ServiceCallbackStateEnum> pCallback, NetworkBehavior pBehavior) {
        if (pBehavior == null) {
            return;
        }

        if (pCallback.ResponseDataOfT == ServiceCallbackStateEnum.RESPONSE_SUCCESS) {
            DestroyPendingObject(pBehavior);
        }
    }

    #endregion
}


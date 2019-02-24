using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;
using ForgeAndUnity.Unity;

namespace ForgeAndUnity.Forge {

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
                FindNetworkSceneTemplate(_sceneName);
            }
        }

        protected virtual void OnTriggerEnter (Collider pOther) {
            if (!NodeManager.IsInitialized || !NodeManager.Instance.IsServer) {
                return;
            }

            AddPendingObject(pOther.gameObject);
            if (_nodeTemplate == null) {
                FindNetworkSceneTemplate(_sceneName);
            }

            if (_nodeTemplate != null) {
                TeleportPendingObjects(_nodeTemplate);
            }
        }

        protected virtual void OnDestroy () {
            // If a lookup is still going on we need to be sure to unsubscribe from any events
            if (_currentLookup != null) {
                _currentLookup.OnResponseOfT -= CurrentLookup_OnResponseOfT;
                _currentLookup.OnTimeout -= CurrentLookup_OnTimeout;
            }
        }

        #endregion

        #region Helpers
        public virtual void FindNetworkSceneTemplate (string pSceneName) {
            if (!NodeManager.IsInitialized || !NodeManager.Instance.IsServer || _currentLookup != null || !_findSceneDelay.HasPassed) {
                return;
            }

            // Lookup if the scene resides on our Node
            if (_nodeTemplate == null) {
                NetworkSceneTemplate foundTemplate = NodeManager.Instance.FindNetworkSceneTemplate(pSceneName, false, true, true, false);
                if (foundTemplate != null) {
                    _nodeTemplate = new NodeNetworkSceneTemplate(NodeManager.Instance.CurrentNode.NodeId, foundTemplate);
                    return;
                }
            }

            // Lookup the NodeMap if there is a static NetworkScene we can find on another Node
            if (_nodeTemplate == null && NodeManager.Instance.NodeMapSO != null) {
                _nodeTemplate = NodeManager.Instance.NodeMapSO.nodeMap.GetNodeTemplateBySceneName(pSceneName);
                if (_nodeTemplate != null) {
                    return;
                }
            }

            // Lookup if the scene is a dynamic NetworkScene on another Node
            if (_nodeTemplate == null && _enableLookup && _currentLookup == null) {
                _currentLookup = NodeManager.Instance.LookUpNetworkSceneTemplate(pSceneName);
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
            if (pGameObject.scene.name != gameObject.scene.name) {
                return;
            }

            NetworkBehavior behavior = pGameObject.GetComponent<NetworkBehavior>();
            if (behavior == null) {
                return;
            }

            INetworkSceneObject networkSceneObject = behavior as INetworkSceneObject;
            if (networkSceneObject == null) {
                return;
            }

            uint networkId = networkSceneObject.GetNetworkId();
            if (_pendingObjects.ContainsKey(networkId)) {
                return;
            }

            _pendingObjects.Add(networkId, behavior);
        }

        public virtual void TeleportPendingObjects (NodeNetworkSceneTemplate pNodeTemplate) {
            if (pNodeTemplate == null || !NodeManager.IsInitialized || !NodeManager.Instance.IsServer) {
                return;
            }

            foreach (var item in _pendingObjects.Values) {
                InstantiatePendingObject(pNodeTemplate, item);
            }

            _pendingObjects.Clear();
        }

        public virtual void InstantiatePendingObject (NodeNetworkSceneTemplate pNodeTemplate, NetworkBehavior pBehavior) {
            if (pNodeTemplate == null || pBehavior == null) {
                return;
            }

            INetworkSceneObject networkSceneObject = pBehavior as INetworkSceneObject;
            NetworkObject nObj = networkSceneObject.GetNetworkObject();
            if (nObj == null) {
                return;
            }

            if (pNodeTemplate.NodeId == NodeManager.Instance.CurrentNode.NodeId) {
                NetworkBehavior behavior = NodeManager.Instance.InstantiateInScene(pNodeTemplate.SceneName, nObj.CreateCode, (pBehavior as IRPCSerializable), _teleportTarget.position, _teleportTarget.rotation);
                if (behavior == null) {
                    return;
                }

                pBehavior.gameObject.SetActive(false);
                SendNetworkSceneToPlayer(networkSceneObject, pNodeTemplate);
                DestroyPendingObject(networkSceneObject);
            } else {
                ServiceCallback<RPCInstantiateInNode, ServiceCallbackStateEnum> callback = NodeManager.Instance.InstantiateInNode(pNodeTemplate.NodeId, pNodeTemplate.SceneName, nObj.CreateCode, (pBehavior as IRPCSerializable), _teleportTarget.position, _teleportTarget.rotation);
                if (callback.State == ServiceCallbackStateEnum.AWAITING_RESPONSE) {
                    // While the request is progressing this NetworkSceneTeleporter could have been destroyed.
                    NetworkSceneTeleporter tmpTeleporter = this;
                    callback.OnResponseOfT += (pResponseTime, pResponseDataOfT, pSender) => {
                        if (tmpTeleporter == null || pBehavior == null || networkSceneObject == null || pNodeTemplate == null) {
                            return;
                        }

                        if (pResponseDataOfT == ServiceCallbackStateEnum.RESPONSE_SUCCESS) {
                            pBehavior.gameObject.SetActive(false);
                            tmpTeleporter.SendNetworkSceneToPlayer(networkSceneObject, pNodeTemplate);
                            tmpTeleporter.DestroyPendingObject(networkSceneObject);
                        }
                    };
                }
            }
        }

        public virtual bool SendNetworkSceneToPlayer (INetworkSceneObject pNetworkSceneObject, NodeNetworkSceneTemplate pNodeTemplate) {
            if (pNetworkSceneObject == null || pNetworkSceneObject.Manager == null || pNodeTemplate == null) {
                return false;
            }

            INetworkScenePlayer networkScenePlayer = pNetworkSceneObject as INetworkScenePlayer;
            if (networkScenePlayer == null) {
                return false;
            }

            return pNetworkSceneObject.Manager.ChangePlayerNetworkScene(pNodeTemplate, networkScenePlayer.Player);
        }

        public virtual bool DestroyPendingObject (INetworkSceneObject pNetworkSceneObject) {
            if (pNetworkSceneObject == null) {
                return false;
            }

            NetworkObject nObj = pNetworkSceneObject.GetNetworkObject();
            if (nObj == null) {
                return false;
            }

            nObj.Destroy();
            return true;
        }

        #endregion

        #region Events
        protected virtual void CurrentLookup_OnResponseOfT (float pResponseTime, NodeNetworkSceneTemplate pResponseDataOfT, ServiceCallback<NodeNetworkSceneTemplate> pCallback) {
            _currentLookup.OnResponseOfT -= CurrentLookup_OnResponseOfT;
            _currentLookup.OnTimeout -= CurrentLookup_OnTimeout;
            _currentLookup = null;

            // Only assign our result if there has been nothing found in the meantime!
            if (_nodeTemplate == null && pCallback.State == ServiceCallbackStateEnum.RESPONSE_SUCCESS) {
                _nodeTemplate = pResponseDataOfT;
            }

            if (_nodeTemplate == null) {
                _pendingObjects.Clear();
            } else {
                TeleportPendingObjects(_nodeTemplate);
            }
        }

        protected virtual void CurrentLookup_OnTimeout (ServiceCallback pSender) {
            _currentLookup.OnResponseOfT -= CurrentLookup_OnResponseOfT;
            _currentLookup.OnTimeout -= CurrentLookup_OnTimeout;
            _currentLookup = null;
            _pendingObjects.Clear();
        }

        #endregion
    }
}


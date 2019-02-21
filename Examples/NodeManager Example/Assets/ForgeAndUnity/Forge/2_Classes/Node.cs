using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Describes a Node in a multiserver-setting.
/// </summary>
[System.Serializable]
public class Node : IRPCSerializable<RPCNode> {
    //Fields
    [SerializeField] protected uint                             _nodeId;
    [SerializeField] protected string                           _nodeName;
    [SerializeField] protected bool                             _isMasterNode;
    [SerializeField] protected ushort                           _dynamicPortMin;
    [SerializeField] protected ushort                           _dynamicPortMax;
    [SerializeField] protected NetworkSceneManagerSetting       _masterNodeSetting;
    [SerializeField] protected NetworkSceneTemplate[]           _initWithNetworkScenes;
    protected Dictionary<string, NetworkSceneTemplate>          _networkSceneTemplates;

    public uint                                                 NodeId                              { get { return _nodeId; } }
    public string                                               NodeName                            { get { return _nodeName; } }
    public bool                                                 IsMasterNode                        { get { return _isMasterNode; } }
    public ushort                                               DynamicPortMin                      { get { return _dynamicPortMin; } }
    public ushort                                               DynamicPortMax                      { get { return _dynamicPortMax; } }
    public NetworkSceneManagerSetting                           MasterNodeSetting                   { get { return _masterNodeSetting; } }
    public NetworkSceneTemplate[]                               InitWithNetworkScenes               { get { return _initWithNetworkScenes; } }
    public Dictionary<string, NetworkSceneTemplate>             NetworkSceneTemplates               { get { return _networkSceneTemplates; } }


    //Functions
    public Node () {
        _networkSceneTemplates = new Dictionary<string, NetworkSceneTemplate>();
    }

    public Node (uint pNodeId, string pNodeName, bool pIsMasterNode, ushort pPortRangeMin, ushort pPortRangeMax, NetworkSceneManagerSetting pMasterNodeSetting, NetworkSceneTemplate[] pInitWithNetworkSceneTemplates) : this() {
        _nodeId = pNodeId;
        _nodeName = pNodeName;
        _isMasterNode = pIsMasterNode;
        _dynamicPortMin = pPortRangeMin;
        _dynamicPortMax = pPortRangeMax;
        _masterNodeSetting = pMasterNodeSetting;
        _initWithNetworkScenes = pInitWithNetworkSceneTemplates;
        Init(_initWithNetworkScenes);
    }

    public Node (RPCNode pNode) : this() {
        FromRPC(pNode);
    }

    #region Init
    public virtual void Init () {
        Init(_initWithNetworkScenes);
    }

    public virtual void Init(NetworkSceneTemplate[] pNetworkSceneTemplates) {
        _networkSceneTemplates.Clear();
        AddNetworkSceneTemplates(pNetworkSceneTemplates);
    }

    #endregion

    #region Helpers
    public virtual NetworkSceneTemplate GetBySceneName (string pSceneName) {
        NetworkSceneTemplate networkSceneTemplate;
        _networkSceneTemplates.TryGetValue(pSceneName, out networkSceneTemplate);
        return networkSceneTemplate;
    }

    public virtual bool ContainsNetworkSceneTemplate (string pSceneName) {
        return (GetBySceneName(pSceneName) != null);
    }

    public virtual void AddNetworkSceneTemplates (NetworkSceneTemplate[] pNetworkSceneTemplates) {
        if (pNetworkSceneTemplates == null) {
            return;
        }

        for (int i = 0; i < pNetworkSceneTemplates.Length; i++) {
            if (pNetworkSceneTemplates[i] == null || _networkSceneTemplates.ContainsKey(pNetworkSceneTemplates[i].SceneName)) {
                continue;
            }

            _networkSceneTemplates.Add(pNetworkSceneTemplates[i].SceneName, pNetworkSceneTemplates[i]);
        }
    }

    protected virtual RPCNetworkSceneTemplate[] ToNetworkSceneTemplatesRPC () {
        List<RPCNetworkSceneTemplate> networkSceneTemplatesRPC = new List<RPCNetworkSceneTemplate>();
        foreach (var item in _networkSceneTemplates) {
            networkSceneTemplatesRPC.Add(item.Value.ToRPC());
        }

        return networkSceneTemplatesRPC.ToArray();
    }

    #endregion

    #region Serialization
    public virtual void FromRPC (RPCNode pNetworkSceneNodeRPC) {
        _nodeId = pNetworkSceneNodeRPC.nodeId;
        _nodeName = pNetworkSceneNodeRPC.nodeName;
        _isMasterNode = pNetworkSceneNodeRPC.isMasterNode;
        _dynamicPortMin = pNetworkSceneNodeRPC.portRangeMin;
        _dynamicPortMax = pNetworkSceneNodeRPC.portRangeMax;
        _masterNodeSetting = new NetworkSceneManagerSetting();
        _masterNodeSetting.FromRPC(pNetworkSceneNodeRPC.masterNodeSettingRPC);
        if (pNetworkSceneNodeRPC.networkSceneTemplatesRPC == null) {
            Init(null);
        }

        List<NetworkSceneTemplate> networkSceneTemplates = new List<NetworkSceneTemplate>(pNetworkSceneNodeRPC.networkSceneTemplatesRPC.Length);
        for (int i = 0; i < pNetworkSceneNodeRPC.networkSceneTemplatesRPC.Length; i++) {
            NetworkSceneTemplate networkSceneTemplate = new NetworkSceneTemplate();
            networkSceneTemplate.FromRPC(pNetworkSceneNodeRPC.networkSceneTemplatesRPC[i]);
            networkSceneTemplates.Add(networkSceneTemplate);
        }

        Init(networkSceneTemplates.ToArray());
    }

    public virtual RPCNode ToRPC () {
        RPCNode networkSceneNodeRPC = new RPCNode();
        networkSceneNodeRPC.nodeId = _nodeId;
        networkSceneNodeRPC.nodeName = _nodeName;
        networkSceneNodeRPC.isMasterNode = _isMasterNode;
        networkSceneNodeRPC.portRangeMin = _dynamicPortMin;
        networkSceneNodeRPC.portRangeMax = _dynamicPortMax;
        networkSceneNodeRPC.masterNodeSettingRPC = _masterNodeSetting.ToRPC();
        networkSceneNodeRPC.networkSceneTemplatesRPC = ToNetworkSceneTemplatesRPC();
        return networkSceneNodeRPC;
    }

    public virtual byte[] ToByteArray () {
        return ToRPC().ObjectToByteArray();
    }

    public virtual void FromByteArray (byte[] pByteArray) {
        FromRPC(pByteArray.ByteArrayToObject<RPCNode>());
    }

    #endregion
}

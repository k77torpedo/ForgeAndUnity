using UnityEngine;

/// <summary>
/// A template for creating a scene on another Node through a <see cref="NodeManager"/>. Usually returned by a <see cref="NodeService.LookupScene(uint, string)"/>.
/// </summary>
[System.Serializable]
public class NodeNetworkSceneTemplate : NetworkSceneTemplate {
    //Fields
    [SerializeField] protected uint     _nodeId;

    public uint                         NodeId { get { return _nodeId; } set { _nodeId = value; } }


    //Functions
    public NodeNetworkSceneTemplate() : this(0, -1, string.Empty, RPCVector3.zero, null) { }

    public NodeNetworkSceneTemplate (uint pNodeId, int pBuildIndex, string pSceneName, RPCVector3 pSceneOffset, NetworkSceneManagerSetting pNetworkSceneManagerSetting)
        : base (pBuildIndex, pSceneName, pSceneOffset, pNetworkSceneManagerSetting) {
        _nodeId = pNodeId;
    }

    public NodeNetworkSceneTemplate (uint pNodeId, NetworkSceneTemplate pNetworkSceneTemplate) : base (pNetworkSceneTemplate) {
        _nodeId = pNodeId;
    }

    public NodeNetworkSceneTemplate (uint pNodeId, RPCNetworkSceneTemplate pNetworkSceneTemplateRPC) : base (pNetworkSceneTemplateRPC) {
        _nodeId = pNodeId;
    }
}

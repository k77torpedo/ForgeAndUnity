using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Describes all <see cref="Node"/>s in a multiserver-setting.
/// </summary>
[System.Serializable]
public class NodeMap : IRPCSerializable<RPCNodeMap> {
    //Fields
    [SerializeField] protected Node[]           _initWithNodes;
    protected Dictionary<uint, Node>            _nodes;
    protected Dictionary<string, uint>          _nameToId;

    public Node[]                               InitWithNodes           { get { return _initWithNodes; } }
    public Dictionary<uint, Node>               Nodes                   { get { return _nodes; } }


    //Functions
    public NodeMap () {
        _nodes = new Dictionary<uint, Node>();
        _nameToId = new Dictionary<string, uint>();
    }

    public NodeMap (Node[] pInitWithNodes) : this() {
        _initWithNodes = pInitWithNodes;
        Init(_initWithNodes);
    }

    #region Init
    public virtual void Init () {
        Init(_initWithNodes);
    }

    public virtual void Init (Node[] pNodes) {
        _nodes.Clear();
        AddNodes(pNodes);
    }

    #endregion

    #region Helpers
    public virtual Node GetByNodeName (string pNodeName) {
        return GetByNodeId(NameToId(pNodeName));
    }

    public virtual Node GetByNodeId (uint pNodeId) {
        Node node;
        _nodes.TryGetValue(pNodeId, out node);
        return node;
    }

    public virtual Node GetBySceneName (string pSceneName) {
        foreach (var item in _nodes.Values) {
            if (item.ContainsNetworkSceneTemplate(pSceneName)) {
                return item;
            }
        }

        return null;
    }

    public virtual NetworkSceneTemplate GetTemplateBySceneName (string pSceneName) {
        NetworkSceneTemplate template;
        foreach (var item in _nodes.Values) {
            template = item.GetBySceneName(pSceneName);
            if (template != null) {
                return template;
            }
        }

        return null;
    }

    public virtual NodeNetworkSceneTemplate GetNodeTemplateBySceneName (string pSceneName) {
        NetworkSceneTemplate template;
        foreach (var item in _nodes.Values) {
            template = item.GetBySceneName(pSceneName);
            if (template != null) {
                return new NodeNetworkSceneTemplate(item.NodeId, template);
            }
        }

        return null;
    }

    public virtual Node GetMasterNode () {
        foreach (var item in _nodes.Values) {
            if (item.IsMasterNode) {
                return item;
            }
        }

        return null;
    }

    public virtual uint NameToId (string pNodeName) {
        uint nodeId;
        _nameToId.TryGetValue(pNodeName, out nodeId);
        return nodeId;
    }

    public virtual Node GetNodeWithTemplateOnly (string pSceneName) {
        //This creates a new and independent NetworkSceneNode with only the NetworkSceneTemplate in it.
        Node node = GetBySceneName(pSceneName);
        if (node == null) {
            return null;
        }

        NetworkSceneTemplate networkSceneTemplate = node.GetBySceneName(pSceneName);
        RPCNode nodeRPC = node.ToRPC();
        nodeRPC.networkSceneTemplatesRPC = new RPCNetworkSceneTemplate[] { networkSceneTemplate.ToRPC() };
        return new Node(nodeRPC);
    }

    public virtual void AddNodes(Node[] pNodes) {
        if (pNodes == null) {
            return;
        }

        for (int i = 0; i < pNodes.Length; i++) {
            if (pNodes[i] == null 
                || _nodes.ContainsKey(pNodes[i].NodeId)
                || _nameToId.ContainsKey(pNodes[i].NodeName)) {
                continue;
            }

            pNodes[i].Init();
            _nodes.Add(pNodes[i].NodeId, pNodes[i]);
            _nameToId.Add(pNodes[i].NodeName, pNodes[i].NodeId);
        }
    }

    #endregion

    #region Serialization
    public virtual void FromRPC (RPCNodeMap pNodeMapRPC) {
        if (pNodeMapRPC.nodes == null) {
            Init(null);
        }

        List<Node> nodes = new List<Node>(pNodeMapRPC.nodes.Length);
        for (int i = 0; i < pNodeMapRPC.nodes.Length; i++) {
            nodes.Add(new Node(pNodeMapRPC.nodes[i]));
        }

        Init(nodes.ToArray());
    }

    public virtual RPCNodeMap ToRPC () {
        List<RPCNode> nodesRPC = new List<RPCNode>(_nodes.Count);
        foreach (var item in _nodes.Values) {
            nodesRPC.Add(item.ToRPC());
        }

        return new RPCNodeMap() {
            nodes = nodesRPC.ToArray()
        };
    }

    public virtual byte[] ToByteArray () {
        return ToRPC().ObjectToByteArray();
    }

    public virtual void FromByteArray (byte[] pByteArray) {
        FromRPC(pByteArray.ByteArrayToObject<RPCNodeMap>());
    }

    #endregion
}

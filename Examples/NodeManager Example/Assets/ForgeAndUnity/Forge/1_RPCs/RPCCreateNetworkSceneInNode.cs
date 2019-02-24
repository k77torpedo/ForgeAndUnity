namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Lightweight container for instantiating a 'NetworkScene' in another <see cref="Node"/> over RPCs.
    /// </summary>
    [System.Serializable]
    public struct RPCCreateNetworkSceneInNode {
        //Fields
        public uint targetNodeId;
        public RPCNetworkSceneTemplate template;
        public bool autoAssignIp;
        public bool autoAssignPort;
        public byte[] networkSceneMetaData;
    }
}
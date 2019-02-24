namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Lightweight container for serializing and deserializing a <see cref="Node"/> over RPCs.
    /// </summary>
    [System.Serializable]
    public struct RPCNode {
        //Fields
        public uint nodeId;
        public string nodeName;
        public bool isMasterNode;
        public ushort portRangeMin;
        public ushort portRangeMax;
        public RPCNetworkSceneManagerSetting masterNodeSettingRPC;
        public RPCNetworkSceneTemplate[] networkSceneTemplatesRPC;
    }
}

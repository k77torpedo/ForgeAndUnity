namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Lightweight container for serializing and deserializing <see cref="NodeMap"/> over RPCs.
    /// </summary>
    [System.Serializable]
    public struct RPCNodeMap {
        //Fields
        public RPCNode[] nodes;
    }
}

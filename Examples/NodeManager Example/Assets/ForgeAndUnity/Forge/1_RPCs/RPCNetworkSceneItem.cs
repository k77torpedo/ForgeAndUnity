namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Lightweight container for serializing and deserializing <see cref="NetworkSceneItem"/> over RPCs.
    /// </summary>
    [System.Serializable]
    public struct RPCNetworkSceneItem {
        //Fields
        public RPCNetworkSceneTemplate sceneTemplate;
    }
}

using BeardedManStudios.Forge.Networking.Unity;

namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Lightweight container for instantiating a <see cref="NetworkBehavior"/> in another <see cref="Node"/> over RPCs.
    /// </summary>
    [System.Serializable]
    public struct RPCInstantiateInNode {
        //Fields
        public uint targetNodeId;
        public string sceneName;
        public int createCode;
        public byte[] behaviorData;
        public RPCVector3 position;
        public RPCQuaternion rotation;
        public bool sendTransform;
    }
}

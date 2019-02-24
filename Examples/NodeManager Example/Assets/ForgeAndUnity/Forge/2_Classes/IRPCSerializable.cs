namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Interface for serializing an object for transmition and re-instantiation over the network via a Remote Procedure Call (RPC).
    /// </summary>
    public interface IRPCSerializable {
        byte[] ToByteArray ();
        void FromByteArray (byte[] pByteArray);
    }
}

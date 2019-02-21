/// <summary>
/// Lightweight container for serializing and deserializing <see cref="ServiceCallback"/> over RPCs.
/// </summary>
[System.Serializable]
public struct RPCServiceCallback {
    //Fields
    public uint callbackId;
    public uint sourceNodeId;
    public ServiceCallbackStateEnum state;
    public byte[] data;
}

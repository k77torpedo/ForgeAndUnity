/// <summary>
/// Lightweight container for serializing and deserializing <see cref="NetworkSceneManagerSetting"/> over RPCs.
/// </summary>
[System.Serializable]
public struct RPCNetworkSceneManagerSetting {
    //Fields
    public int maxConnections;
    public bool useTCP;
    public bool useMainThreadManagerForRPCs;
    public RPCNetworkSceneManagerEndpoint hostAddressRPC;
    public RPCNetworkSceneManagerEndpoint hostNATAddressRPC;
    public RPCNetworkSceneManagerEndpoint clientAddressRPC;
    public RPCNetworkSceneManagerEndpoint clientNATAddressRPC;
}

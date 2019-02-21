/// <summary>
/// Lightweight container for serializing and deserializing <see cref="NetworkSceneManagerEndpoint"/> over RPCs.
/// </summary>
[System.Serializable]
public struct RPCNetworkSceneManagerEndpoint {
    //Fields
    public string ip;
    public ushort port;


    //Functions
    public static bool operator == (RPCNetworkSceneManagerEndpoint a, RPCNetworkSceneManagerEndpoint b) {
        return (a.ip == b.ip && a.port == b.port);
    }

    public static bool operator != (RPCNetworkSceneManagerEndpoint a, RPCNetworkSceneManagerEndpoint b) {
        return (a.ip != b.ip || a.port != b.port);
    }
}

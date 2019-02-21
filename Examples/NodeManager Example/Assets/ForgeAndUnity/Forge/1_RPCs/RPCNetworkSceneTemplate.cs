/// <summary>
/// Lightweight container for serializing and deserializing <see cref="NetworkSceneTemplate"/> over RPCs.
/// </summary>
[System.Serializable]
public struct RPCNetworkSceneTemplate {
    //Fields
    public int buildIndex;
    public string sceneName;
    public RPCVector3 sceneOffset;
    public RPCNetworkSceneManagerSetting settings;
}

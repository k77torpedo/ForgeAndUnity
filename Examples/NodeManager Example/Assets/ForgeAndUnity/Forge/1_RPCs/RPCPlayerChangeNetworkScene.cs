using BeardedManStudios.Forge.Networking;

/// <summary>
/// Lightweight container for serializing and deserializing data over RPCs to instruct a 
/// <see cref="NetworkingPlayer"/> to change scenes. Only contains information neccessary to know for the client.
/// </summary>
[System.Serializable]
public struct RPCPlayerChangeNetworkScene {
    //Fields
    public int buildIndex;
    public string sceneName;
    public RPCVector3 sceneOffset;
    public bool useTCP;
    public bool useMainThreadManagerForRPCs;
    public RPCNetworkSceneManagerEndpoint clientAddress;
    public RPCNetworkSceneManagerEndpoint clientNATAddress;


    //Functions
    public static RPCPlayerChangeNetworkScene FromNetworkSceneTemplate(NetworkSceneTemplate pTemplate) {
        return new RPCPlayerChangeNetworkScene() {
            buildIndex = pTemplate.BuildIndex,
            sceneName = pTemplate.SceneName,
            sceneOffset = pTemplate.SceneOffset,
            useTCP = pTemplate.Settings.UseTCP,
            useMainThreadManagerForRPCs = pTemplate.Settings.UseMainThreadManagerForRPCs,
            clientAddress = pTemplate.Settings.ClientAddress.ToRPC(),
            clientNATAddress = pTemplate.Settings.ClientNATAddress.ToRPC()
        };
    }

    public static NetworkSceneTemplate ToNetworkSceneTemplate (RPCPlayerChangeNetworkScene pChangeSceneRPC) {
        NetworkSceneTemplate template = new NetworkSceneTemplate(pChangeSceneRPC.buildIndex, pChangeSceneRPC.sceneName, pChangeSceneRPC.sceneOffset, new NetworkSceneManagerSetting());
        template.Settings.UseTCP = pChangeSceneRPC.useTCP;
        template.Settings.UseMainThreadManagerForRPCs = pChangeSceneRPC.useMainThreadManagerForRPCs;
        template.Settings.ClientAddress.FromRPC(pChangeSceneRPC.clientAddress);
        template.Settings.ClientNATAddress.FromRPC(pChangeSceneRPC.clientNATAddress);
        return template;
    }
}

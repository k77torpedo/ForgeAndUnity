using UnityEngine;

/// <summary>
/// A template for creating a <see cref="NetworkSceneItem"/> through a <see cref="NodeManager"/>.
/// </summary>
[System.Serializable]
public class NetworkSceneTemplate : IRPCSerializable<RPCNetworkSceneTemplate> {
    //Fields
    [SerializeField] protected int                          _buildIndex;
    [SerializeField] protected string                       _sceneName;
    [SerializeField] protected RPCVector3                   _sceneOffset;
    [SerializeField] protected NetworkSceneManagerSetting   _settings;

    public int                                              BuildIndex          { get { return _buildIndex; } set { _buildIndex = value; } }
    public string                                           SceneName           { get { return _sceneName; } set { _sceneName = value; } }
    public RPCVector3                                       SceneOffset         { get { return _sceneOffset; } set { _sceneOffset = value; } }
    public NetworkSceneManagerSetting                       Settings            { get { return _settings; } set { _settings = value; } }


    //Functions
    public NetworkSceneTemplate () : this (-1, string.Empty, RPCVector3.zero, null) { }

    public NetworkSceneTemplate (int pBuildIndex, string pSceneName, RPCVector3 pSceneOffset, NetworkSceneManagerSetting pSetting) {
        _buildIndex = pBuildIndex;
        _sceneName = pSceneName;
        _sceneOffset = pSceneOffset;
        _settings = pSetting;
    }

    public NetworkSceneTemplate (NetworkSceneTemplate pTemplate) 
        : this (pTemplate.BuildIndex, pTemplate.SceneName, pTemplate.SceneOffset, new NetworkSceneManagerSetting(pTemplate.Settings)) {
     }

    public NetworkSceneTemplate (RPCNetworkSceneTemplate pTemplateRPC) {
        FromRPC(pTemplateRPC);
    }

    #region Serialization
    public virtual RPCNetworkSceneTemplate ToRPC () {
        return new RPCNetworkSceneTemplate() {
            buildIndex = _buildIndex,
            sceneName = _sceneName,
            sceneOffset = _sceneOffset,
            settings = ((_settings != null) ? _settings.ToRPC() : new RPCNetworkSceneManagerSetting())
        };
    }

    public virtual void FromRPC (RPCNetworkSceneTemplate pTemplateRPC) {
        _buildIndex = pTemplateRPC.buildIndex;
        _sceneName = pTemplateRPC.sceneName;
        _sceneOffset = pTemplateRPC.sceneOffset;
        _settings = new NetworkSceneManagerSetting(pTemplateRPC.settings);
    }

    public virtual byte[] ToByteArray () {
        return ToRPC().ObjectToByteArray();
    }

    public virtual void FromByteArray (byte[] pByteArray) {
        FromRPC(pByteArray.ByteArrayToObject<RPCNetworkSceneTemplate>());
    }

    #endregion
}

using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;

/// <summary>
/// Implementing this interface on a <see cref="NetworkBehavior"/> will add 
/// the responsible <see cref="NetworkSceneManager"/> to it during instantiation for ease of access.
/// Also provides anonymised access to the underlying <see cref="NetworkObject"/>.
/// </summary>
public interface INetworkSceneObject {
    //Fields
    NetworkSceneManager Manager { get; set; }

    //Function
    void SetNetworkObject (NetworkObject pNetworkObject);
    NetworkObject GetNetworkObject ();
    uint GetNetworkId ();
}
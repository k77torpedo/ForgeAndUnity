using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;

/// <summary>
/// Indicates a <see cref="NetworkingPlayer"/> controlling the <see cref="NetworkBehavior"/> this interface has been implemented on.
/// The <see cref="Player"/>-property needs to be set manually during instantiation.
/// </summary>
public interface INetworkScenePlayer {
    NetworkingPlayer Player { get; set; }
}

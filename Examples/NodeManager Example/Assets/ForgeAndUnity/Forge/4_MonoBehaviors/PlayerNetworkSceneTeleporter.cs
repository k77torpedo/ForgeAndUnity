using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;

/// <summary>
/// A specialized version of the <see cref="NetworkSceneTeleporter"/> that only moves <see cref="NetworkBehavior"/>s
/// that implement the <see cref="INetworkSceneObject"/>- and <see cref="INetworkScenePlayer"/>-interface.
/// </summary>
public class PlayerNetworkSceneTeleporter : NetworkSceneTeleporter {
    //Functions
    public override void AddPendingObject (GameObject pGameObject) {
        NetworkBehavior behavior = pGameObject.GetComponent<NetworkBehavior>();
        if (behavior == null) {
            return;
        }

        INetworkSceneObject networkSceneObject = behavior as INetworkSceneObject;
        INetworkScenePlayer player = behavior as INetworkScenePlayer;
        uint networkId = networkSceneObject.GetNetworkId();
        if (networkSceneObject == null || player == null || _pendingObjects.ContainsKey(networkId)) {
            return;
        }

        _pendingObjects.Add(networkId, behavior);
    }

    protected override void DestroyPendingObject (NetworkBehavior pBehavior) {
        INetworkSceneObject networkSceneObject = pBehavior as INetworkSceneObject;
        INetworkScenePlayer networkScenePlayer = pBehavior as INetworkScenePlayer;
        bool success = networkSceneObject.Manager.ChangePlayerNetworkScene(_nodeTemplate, networkScenePlayer.Player);
        if (!success) {
            return;
        }

        base.DestroyPendingObject(pBehavior);
    }
}

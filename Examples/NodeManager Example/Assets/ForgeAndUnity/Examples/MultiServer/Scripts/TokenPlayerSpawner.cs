using System;
using UnityEngine;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;

/// <summary>
/// Creates a new <see cref="TokenPlayer"/> when a <see cref="NetworkingPlayer"/> connects to 
/// the active scene and assigns it to that <see cref="NetworkingPlayer"/> via a unique <see cref="TokenPlayer.PlayerToken"/>.
/// </summary>
public class TokenPlayerSpawner : MonoBehaviour {
    //Fields
    NetworkSceneManager _manager;


    //Functions
    #region Unity
    void Start () {
        if (!NodeManager.IsInitialized || !NodeManager.Instance.IsServer) {
            return;
        }

        _manager = NodeManager.Instance.FindNetworkSceneManager(gameObject.scene.name);
        if (_manager == null || !_manager.HasNetworker) {
            return;
        }

        _manager.Networker.playerAccepted += Networker_playerAccepted;
    }

    #endregion

    #region Events
    void Networker_playerAccepted (NetworkingPlayer pPlayer, NetWorker pSender) {
        MainThreadManager.Run(() => {
            if (_manager == null) {
                return;
            }

            TokenPlayer playerBehavior = _manager.InstantiateNetworkBehavior("TokenPlayer", null, transform.position, transform.rotation) as TokenPlayer;
            if (playerBehavior == null) {
                return;
            }

            playerBehavior.PlayerToken = Guid.NewGuid().ToString();
            playerBehavior.Player = pPlayer;
        });
    }

    #endregion
}

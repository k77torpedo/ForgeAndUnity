using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking;

[RequireComponent(typeof(NavMeshAgent))]
public class Player : PlayerBehavior, INetworkSceneObject, IRPCSerializable {
    //Fields
    public static string playerGUID_Client; // Only for the client: we save our GUID here.

    public string playerGUID_Server; // Per GameObject: The Guid will be injected from outside and maintained when changing NetworkScenes
    public NetworkingPlayer player; // Per GameObject: The NetworkingPlayer will be injected from outside
    NavMeshAgent _agent;

    public NetworkSceneManager Manager { get; set; }

    //Events
    public delegate void GetPlayerGUIDEvent (RpcArgs pArgs);
    public event GetPlayerGUIDEvent OnGetPlayerGUID;


    //Functions
    #region Unity
    void Awake () {
        _agent = GetComponent<NavMeshAgent>();
        _agent.enabled = false;
    }

    protected override void NetworkStart () {
        base.NetworkStart();
        RegisterEventsServerClient();
        if (networkObject.IsServer) {
            RegisterEventsServer();
        } else {
            RegisterEventsClient();
            GetPlayerGUID(playerGUID_Client);
        }

        _agent.enabled = true;
    }

    void Update () {
        if (networkObject == null) {
            return;
        }

        if (!networkObject.IsOwner) {
            transform.position = networkObject.position;
            transform.rotation = networkObject.rotation;
            return;
        }

        ProcessPlayerInput();
        networkObject.position = transform.position;
        networkObject.rotation = transform.rotation;
    }

    #endregion

    #region Register Events
    void RegisterEventsServer () {
        OnGetPlayerGUID += OnGetPlayerGUID_Server;
    }

    void RegisterEventsClient () {
        OnGetPlayerGUID += OnGetPlayerGUID_Client;
    }

    void RegisterEventsServerClient () {
        OnGetPlayerGUID += OnGetPlayerGUID_ServerClient;
    }

    #endregion

    #region GetPlayerGUID
    public void GetPlayerGUID (string pPlayerGUID) {
        networkObject.SendRpc(RPC_GET_PLAYER_G_U_I_D, Receivers.Server, pPlayerGUID);
    }

    public void AssignPlayerGUID(string pPlayerGUID, NetworkingPlayer pPlayer) {
        networkObject.SendRpc(pPlayer, RPC_GET_PLAYER_G_U_I_D, pPlayerGUID);
    }

    void OnGetPlayerGUID_Server (RpcArgs pArgs) {
        string playerGUIDClient = pArgs.GetNext<string>();
        if (playerGUIDClient == playerGUID_Server || pArgs.Info.SendingPlayer == player) {
            AssignPlayerGUID(playerGUID_Server, pArgs.Info.SendingPlayer);
        }
    }

    void OnGetPlayerGUID_Client (RpcArgs pArgs) {
        playerGUID_Server = pArgs.GetNext<string>();
        playerGUID_Client = playerGUID_Server;
        networkObject.TakeOwnership();
    }

    void OnGetPlayerGUID_ServerClient (RpcArgs pArgs) {
        // your code here...
    }

    #endregion

    #region Helpers
    public void ProcessPlayerInput () {
        if (Input.GetKeyDown(KeyCode.W)) {
            _agent.SetDestination(transform.position + Vector3.forward);
        } else if (Input.GetKeyDown(KeyCode.A)) {
            _agent.SetDestination(transform.position + Vector3.left);
        } else if (Input.GetKeyDown(KeyCode.S)) {
            _agent.SetDestination(transform.position + Vector3.back);
        } else if (Input.GetKeyDown(KeyCode.D)) {
            _agent.SetDestination(transform.position + Vector3.right);
        }
    }

    #endregion

    #region RPC-Callbacks
    public override void GetPlayerGUID (RpcArgs pArgs) {
        if (OnGetPlayerGUID != null) {
            OnGetPlayerGUID(pArgs);
        }
    }

    #endregion

    #region INetworkSceneObject-Implementation
    public void SetNetworkObject (NetworkObject pNetworkObject) {
        networkObject = pNetworkObject as PlayerNetworkObject;
    }

    public NetworkObject GetNetworkObject () {
        return networkObject;
    }

    public uint GetNetworkId () {
        return (networkObject != null) ? networkObject.NetworkId : 0;
    }

    #endregion

    #region IRPCSerializable-Implementation
    public byte[] ToByteArray () {
        return playerGUID_Server.ObjectToByteArray();
    }

    public void FromByteArray (byte[] pByteArray) {
        playerGUID_Server = pByteArray.ByteArrayToObject<string>();
    }

    #endregion
}

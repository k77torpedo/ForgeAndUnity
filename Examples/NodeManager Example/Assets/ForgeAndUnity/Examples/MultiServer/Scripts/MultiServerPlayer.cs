using UnityEngine;
using UnityEngine.AI;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking;

/// <summary>
/// 
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent))]
public class MultiServerPlayer : MultiServerPlayerBehavior, INetworkScenePlayer, INetworkSceneObject, IRPCSerializable {
    //Fields
    public static string playerId_Client; // Only for the client: we save the initial GUID we got from our server here. This variable is 'static' to persist between NetworkScenes.

    public string playerId_Server; // Per GameObject: The Guid will be injected from outside and maintained when changing NetworkScenes
    NavMeshAgent _agent;

    public NetworkSceneManager Manager { get; set; }
    public NetworkingPlayer Player { get; set; }

    //Events
    public delegate void CheckMultiServerPlayerIdEvent (RpcArgs pArgs);
    public event CheckMultiServerPlayerIdEvent OnCheckMultiServerPlayerId;

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
            CheckMultiServerPlayerId(playerId_Client);
        }

        _agent.enabled = true;
        networkObject.positionInterpolation.Enabled = false;
        networkObject.positionChanged += WarpToFirstValue;
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
        OnCheckMultiServerPlayerId += OnCheckMultiServerPlayerId_Server;
    }

    void RegisterEventsClient () {
        OnCheckMultiServerPlayerId += OnCheckMultiServerPlayerId_Client;
    }

    void RegisterEventsServerClient () {
        OnCheckMultiServerPlayerId += OnCheckMultiServerPlayerId_ServerClient;
    }

    #endregion

    #region CheckMultiServerPlayerId
    public void AssignPlayerOwnership (string pPlayerId, NetworkingPlayer pPlayer) {
        pPlayer.disconnected += (sender) => {
            if (networkObject == null) {
                return;
            }

            networkObject.Destroy();
        };

        networkObject.AssignOwnership(pPlayer);
        networkObject.SendRpc(pPlayer, RPC_CHECK_MULTI_SERVER_PLAYER_ID, (pPlayerId ?? string.Empty).ObjectToByteArray());
    }

    void CheckMultiServerPlayerId (string pPlayerId) {
        networkObject.SendRpc(RPC_CHECK_MULTI_SERVER_PLAYER_ID, Receivers.Server, (pPlayerId ?? string.Empty).ObjectToByteArray());
    }

    void OnCheckMultiServerPlayerId_Server (RpcArgs pArgs) {
        byte[] data = pArgs.GetNext<byte[]>();
        string playerGUIDClient = data.ByteArrayToObject<string>();
        if (playerId_Server == playerGUIDClient || (string.IsNullOrEmpty(playerGUIDClient) && Player == pArgs.Info.SendingPlayer)) {
            Player = pArgs.Info.SendingPlayer;
            AssignPlayerOwnership(playerId_Server, pArgs.Info.SendingPlayer);
        }
    }

    void OnCheckMultiServerPlayerId_Client (RpcArgs pArgs) {
        byte[] data = pArgs.GetNext<byte[]>();
        playerId_Client = data.ByteArrayToObject<string>();
    }

    void OnCheckMultiServerPlayerId_ServerClient (RpcArgs pArgs) {
        // your code here...
    }

    #endregion

    #region Helpers
    public void ProcessPlayerInput () {
        if (Input.GetKey(KeyCode.W)) {
            _agent.SetDestination(transform.position + Vector3.forward);
        } else if (Input.GetKey(KeyCode.A)) {
            _agent.SetDestination(transform.position + Vector3.left);
        } else if (Input.GetKey(KeyCode.S)) {
            _agent.SetDestination(transform.position + Vector3.back);
        } else if (Input.GetKey(KeyCode.D)) {
            _agent.SetDestination(transform.position + Vector3.right);
        }
    }

    #endregion

    #region Events
    public void WarpToFirstValue (Vector3 field, ulong timestep) {
        networkObject.positionChanged -= WarpToFirstValue;
        networkObject.positionInterpolation.Enabled = true;
        networkObject.positionInterpolation.current = networkObject.position;
        networkObject.positionInterpolation.target = networkObject.position;
    }

    #endregion

    #region RPC-Callbacks
    public override void CheckMultiServerPlayerId (RpcArgs pArgs) {
        if (OnCheckMultiServerPlayerId != null) {
            OnCheckMultiServerPlayerId(pArgs);
        }
    }

    #endregion

    #region INetworkSceneObject-Implementation
    public void SetNetworkObject (NetworkObject pNetworkObject) {
        networkObject = pNetworkObject as MultiServerPlayerNetworkObject;
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
        return playerId_Server.ObjectToByteArray();
    }

    public void FromByteArray (byte[] pByteArray) {
        playerId_Server = pByteArray.ByteArrayToObject<string>();
    }

    #endregion
}

using UnityEngine;
using UnityEngine.AI;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking;
using ForgeAndUnity.Forge;

/// <summary>
/// A <see cref="NetworkBehavior"/> that moves randomly and maintains its <see cref="Color"/> across NetworkScenes.
/// </summary>
[RequireComponent(typeof(MeshRenderer), typeof(Rigidbody), typeof(NavMeshAgent))]
public class RandomMovingNPC : RandomMovingNPCBehavior, INetworkSceneObject, IRPCSerializable {
    //Fields
    MeshRenderer                        _meshRenderer;
    NavMeshAgent                        _agent;

    public NetworkSceneManager          Manager             { get; set; }

    //Events
    public delegate void SendColorEvent (RpcArgs pArgs);
    public event SendColorEvent OnSendColor;


    //Functions
    #region Unity
    void Awake() {
        _meshRenderer = GetComponent<MeshRenderer>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.enabled = false;
        SetColor(new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
    }

    protected override void NetworkStart () {
        base.NetworkStart();
        RegisterEventsServerClient();
        if (networkObject.IsServer) {
            RegisterEventsServer();
            SendColorBuffered();
            InvokeRepeating("MoveRandom", 5f, 2f);
        } else {
            RegisterEventsClient();
        }

        _agent.enabled = true;
        networkObject.positionInterpolation.Enabled = false;
        networkObject.positionChanged += WarpToFirstValue;
    }

    void Update() {
        if (networkObject == null) {
            return;
        }

        if (networkObject.IsOwner) {
            networkObject.position = transform.position;
            networkObject.rotation = transform.rotation;
        } else {
            transform.position = networkObject.position;
            transform.rotation = networkObject.rotation;
        }
    }

    #endregion

    #region RegisterEvents
    public void RegisterEventsServer () {
        OnSendColor += OnSendColor_Server;
    }

    public void RegisterEventsClient () {
        OnSendColor += OnSendColor_Client;
    }

    public void RegisterEventsServerClient () {
        OnSendColor += OnSendColor_ServerClient;
    }

    #endregion

    #region SendColor
    public void SendColorBuffered() {
        networkObject.ClearRpcBuffer();
        networkObject.SendRpc(RPC_SEND_COLOR, Receivers.OthersBuffered, ToByteArray());
    }

    void OnSendColor_Server (RpcArgs pArgs) {
        // your code here...
    }

    void OnSendColor_Client (RpcArgs pArgs) {
        FromByteArray(pArgs.GetNext<byte[]>());
    }

    void OnSendColor_ServerClient (RpcArgs pArgs) {
        // your code here...
    }

    #endregion

    #region Helpers
    public void SetColor (Color pColor) {
        if (_meshRenderer == null || pColor == null) {
            return;
        }

        _meshRenderer.material.color = pColor;
    }

    public void MoveRandom () {
        _agent.SetDestination(transform.position + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f)));
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
    public override void SendColor (RpcArgs pArgs) {
        if (OnSendColor != null) {
            OnSendColor(pArgs);
        }
    }

    #endregion

    #region INetworkSceneObject-Implementation
    public void SetNetworkObject (NetworkObject pNetworkObject) {
        networkObject = pNetworkObject as RandomMovingNPCNetworkObject;
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
        Color color = _meshRenderer.material.color;
        return new float[] { color.r, color.g, color.b }.ObjectToByteArray();
    }

    public void FromByteArray (byte[] pByteArray) {
        float[] colors = pByteArray.ByteArrayToObject<float[]>();
        Color color = new Color(colors[0], colors[1], colors[2]);
        _meshRenderer.material.color = color;
    }

    #endregion
}

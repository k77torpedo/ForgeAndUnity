using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking.Unity;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking;
using ForgeAndUnity.Forge;


// TODO: USE FIELDS INSTEAD OF RPC
public class InputListenerPlayer : InputListenerPlayerBehavior {
    //Fields
    public InputListener _listener;
    public Rigidbody _body;

    bool _isOwner;
    bool _isJumping;


    //Functions
    #region Unity
    void Awake () {
        // We subscribe to the InputListener so we can use custom logic on how we actually want to move our GameObject or perform actions like jumping.
        _listener.OnSyncFrame += SyncFrame;
        _listener.OnPerformMovement += PerformMovement;
        _listener.OnPerformAction += PerformAction;
        _listener.OnReconcileFrames += ReconcileFrames;
    }

    protected override void NetworkStart () {
        base.NetworkStart();
        if (!networkObject.IsServer) {
            networkObject.ownerIdChanged += NetworkObject_ownerIdChanged;
        }
    }

    void FixedUpdate () {
        if (networkObject == null) {
            return;
        }

        // Everyone that is not the server or the controlling player will just set the position.
        if (!networkObject.IsServer && !_isOwner) {
            _body.position = networkObject.position;
            return;
        }

        // We advance one frame in the simulation
        _listener.AdvanceFrame();

        // The controlling player saves this Frame with his last input and action recorded.//(_listener.CurrentInputFrame.HasMovement || _listener.CurrentInputFrame.HasActions)
        if (_isOwner) {
            _listener.SaveFrame();
        }

        // The server and the controlling player play the frames they saved.
        if (networkObject.IsServer || _isOwner) {
            _listener.PlayFrame(transform);
        }

        // Reconcile frames when the client is too far away from the server-position.
        if (_isOwner) {
            _listener.ReconcileFrames();
        }

        // The server sets the position on the network for everybody else.
        if (networkObject.IsServer) {
            networkObject.position = transform.position;
        }
    }

    void Update () {
        if (networkObject == null || networkObject.IsServer || !_isOwner) {
            return;
        }

        // We record the players movement based on his input.
        _listener.RecordMovement(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // Record any action the player might take!
        if (Input.GetKeyDown(KeyCode.Space)) {
            _listener.RecordAction(1); // 1 = Jump;
        }

        // Press 'X' during simulation to test Server-Reconciliation!
        if (Input.GetKeyDown(KeyCode.X)) {
            transform.Translate((Vector3.forward + Vector3.left) * 3f);
        }
    }

    void OnCollisionEnter (Collision pCollision) {
        _isJumping = false;
    }

    #endregion

    #region Events
    void SyncFrame () {
        // The player sends the frames he played to the server.
        if (_isOwner) {
            networkObject.SendRpc(RPC_SYNC_INPUTS, Receivers.Server, _listener.DequeueFramesToSend());
        }

        // The server sends back what he played to the controlling player.
        if (networkObject.IsServer) {
            networkObject.SendRpcUnreliable(networkObject.Owner, RPC_SYNC_INPUT_HISTORY, _listener.DequeueLocalInputHistory());
        }
    }

    void PerformMovement (float pSpeed, InputFrame pInputFrame) {
        // Here we provide an implementation on how we want our GameObject to move on input (server and client).
        Vector3 translation = Vector3.ClampMagnitude(new Vector3(pInputFrame.horizontalMovement, 0f, pInputFrame.verticalMovement) * pSpeed * Time.fixedDeltaTime, pSpeed);
        _body.position += translation;
        if (!_isJumping) {
            _body.velocity = translation;
        }
    }

    void PerformAction (InputFrame pInputFrame) {
        // Here we provide an implementation for the actions we recorded (server and client).
        for (int i = 0; i < pInputFrame.actions.Length; i++) {
            switch (pInputFrame.actions[i]) {
                case 1:
                    _body.AddForce(Vector3.up * 10f, ForceMode.Impulse);
                    _isJumping = true;
                    break;
                default:
                    break;
            }
        }
    }

    void ReconcileFrames (float pDistance, InputFrameHistoryItem pLocalItem, InputFrameHistoryItem pServerItem, IEnumerable<InputFrameHistoryItem> pItemsToReconcile) {
        // Here we provide an implementation for the reconciling and replaying frames if anything went wrong.

        // We set our current position to the servers-position and simply replay every input we made and then we should end up where the server is.
        transform.position = new Vector3(pServerItem.xPosition, pServerItem.yPosition, pServerItem.zPosition);
        foreach (var item in pItemsToReconcile) {
            PerformMovement(_listener.Speed, item.inputFrame);
            if (item.inputFrame.HasActions) {
                PerformAction(item.inputFrame);
            }
        }
    }

    void NetworkObject_ownerIdChanged (uint pField, ulong pTimestamp) {
        networkObject.ownerIdChanged -= NetworkObject_ownerIdChanged;
        _isOwner = (NetworkManager.Instance.Networker.Me.NetworkId == pField);
    }

    #endregion

    #region RPC-Callbacks
    public override void SyncInputs (RpcArgs pArgs) {
        _listener.AddFramesToPlay(pArgs.GetNext<byte[]>().ByteArrayToObject<List<InputFrame>>());
    }

    public override void SyncInputHistory (RpcArgs pArgs) {
        _listener.AddAuthoritativeInputHistory(pArgs.GetNext<byte[]>().ByteArrayToObject<List<InputFrameHistoryItem>>());
    }

    #endregion
}


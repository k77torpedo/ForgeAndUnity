using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking;
using ForgeAndUnity.Forge;

public class InputListenerPlayer : InputListenerPlayerBehavior {
    //Fields
    public InputListener _listener;
    public Rigidbody _body;

    //Functions
    #region Unity
    void Awake () {
        _listener.OnSyncFrame += InputListener_OnSyncFrame;
        _listener.OnPerformMovement += InputListener_OnPerformMovement;
        _listener.OnPerformAction += InputListener_OnPerformAction;
    }

    protected override void NetworkStart () {
        base.NetworkStart();
    }

    void FixedUpdate () {
        // In FixedUpdate we handle Movement

        // First: Everyone that is not the server or the controlling player will just set the position
        if (!networkObject.IsServer && !networkObject.IsOwner) {
            transform.position = networkObject.position;
            return;
        }

        // We advance one Frame
        _listener.AdvanceFrame();

        // The controlling player saves this Frame with his last input and action recorded.
        if (networkObject.IsOwner) {
            _listener.SaveFrame();
        }

        // The server and the controlling player play the Frames they saved.
        if (networkObject.IsServer || networkObject.IsOwner) {
            _listener.PlayFrame(transform);
        }

        // Reconcile Frames that have been deemed invalid by the server
        if (networkObject.IsOwner) {
            _listener.ReconcileFrames(transform);
        }

        if (networkObject.IsServer) {
            networkObject.position = transform.position;
        }
    }

    void Update () {
        // In Update we capture the players input as well as actions
        _listener.RecordInput(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // Record any action the player might take like a weapon-attack or jumping!
        if (Input.GetKeyDown(KeyCode.Space)) {
            _listener.RecordAction(1); // Jump!
        }

        // Press 'X' to test Server-Reconciliation!
        if (Input.GetKeyDown(KeyCode.X)) {
            transform.Translate((Vector3.forward + Vector3.left) * 3f);
        }
    }

    #endregion



    #region Events
    void InputListener_OnSyncFrame () {
        // The player sends the Frames he played to the server
        if (networkObject.IsOwner) {
            networkObject.SendRpc(RPC_SYNC_INPUTS, Receivers.Server, _listener.DequeueFramesToSend());
        }

        // The server
        if (networkObject.IsServer) {
            networkObject.SendRpc(networkObject.Owner, RPC_SYNC_INPUT_HISTORY, _listener.DequeueLocalInputHistory());
        }
    }

    void InputListener_OnPerformMovement (float pSpeed, InputFrame pInputFrame) {
        // Here we implement how we want our GameObject to move on input.
        transform.Translate(new Vector3(pInputFrame.horizontalInput * Time.fixedDeltaTime * pSpeed, 0f, pInputFrame.verticalInput * Time.fixedDeltaTime * pSpeed));
    }

    void InputListener_OnPerformAction (InputFrame pInputFrame) {
        // Here we implement the actions the player has issued
        for (int i = 0; i < pInputFrame.actions.Length; i++) {
            switch (pInputFrame.actions[i]) {
                case 1:
                    _body.AddForce(Vector3.up * 10f, ForceMode.Impulse); // Jump!
                    break;
                default:
                    break;
            }
        }
    }

    public override void SyncInputs (RpcArgs pArgs) {
        _listener.AddFramesToPlay(pArgs.GetNext<byte[]>().ByteArrayToObject<List<InputFrame>>());
    }

    public override void SyncInputHistory (RpcArgs pArgs) {
        _listener.AddAuthoritativeInputHistory(pArgs.GetNext<byte[]>().ByteArrayToObject<List<InputFrameHistoryItem>>());
    }

    #endregion
}


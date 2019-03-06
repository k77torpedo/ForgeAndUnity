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

    bool _isJumping;


    //Functions
    #region Unity
    void Awake () {
        // We subscribe to the InputListener so we can use custom logic on how we actually want to move our GameObject or perform actions like jumping.
        _listener.OnSyncFrame += InputListener_OnSyncFrame;
        _listener.OnPerformMovement += InputListener_OnPerformMovement;
        _listener.OnPerformAction += InputListener_OnPerformAction;

        // For Server-Reconciliation we keep it simple and subscribe the same functions for performing movement and actions.
        _listener.OnReconcileMovement += InputListener_OnPerformMovement;
        _listener.OnReconcileAction += InputListener_OnPerformAction;
    }

    void FixedUpdate () {
        // Everyone that is not the server or the controlling player will just set the position.
        if (!networkObject.IsServer && !networkObject.IsOwner) {
            transform.position = networkObject.position;
            return;
        }

        // We advance one frame in the simulation
        _listener.AdvanceFrame();

        // The controlling player saves this Frame with his last input and action recorded.
        if (networkObject.IsOwner && _listener.CurrentInputFrame.HasMovement) {
            _listener.SaveFrame();
        }

        // The server and the controlling player play the frames they saved.
        if (networkObject.IsServer || networkObject.IsOwner) {
            _listener.PlayFrame(transform);
        }

        // Reconcile Frames that have been deemed invalid by the server for the controlling player.
        if (networkObject.IsOwner) {
            _listener.ReconcileFrames(transform);
        }

        // The server sets the position on the network for everybody else.
        if (networkObject.IsServer) {
            networkObject.position = transform.position;
        }
    }

    void Update () {
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
    void InputListener_OnSyncFrame () {
        // The player sends the Frames he played to the server
        if (networkObject.IsOwner) {
            networkObject.SendRpc(RPC_SYNC_INPUTS, Receivers.Server, _listener.DequeueFramesToSend());
        }

        // The server sends back what he played to the controlling player.
        if (networkObject.IsServer) {
            networkObject.SendRpc(networkObject.Owner, RPC_SYNC_INPUT_HISTORY, _listener.DequeueLocalInputHistory());
        }
    }

    void InputListener_OnPerformMovement (float pSpeed, InputFrame pInputFrame) {
        // Here we provide an implementation on how we want our GameObject to move on input.
        //transform.Translate(new Vector3(pInputFrame.horizontalInput * Time.fixedDeltaTime * pSpeed, 0f, pInputFrame.verticalInput * Time.fixedDeltaTime * pSpeed));
        Vector3 translation = Vector3.ClampMagnitude(new Vector3(pInputFrame.horizontalMovement, 0f, pInputFrame.verticalMovement) * pSpeed * Time.fixedDeltaTime, pSpeed);
        _body.position += translation;
        if (!_isJumping) {
            _body.velocity = translation;
        }
    }

    void InputListener_OnPerformAction (InputFrame pInputFrame) {
        // Here we provide an implementation for the actions we recorded.
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


using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking.Unity;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking;
using ForgeAndUnity.Forge;


public class InputListenerPlayer : InputListenerPlayerBehavior {
    //Fields
    public InputListener _listener;
    public Rigidbody _body;

    NetworkingPlayer _owningPlayer;
    bool _isOwner;
    bool _isJumping;
    Vector3 _errorMargin;


    //Functions
    #region Unity
    void Awake () {
        // We subscribe to the InputListener so we can use custom logic on how we actually want to move our GameObject or perform actions like jumping.
        _listener.OnSyncFrame += SyncFrame;
        _listener.OnPlayFrame += PlayFrame;
        _listener.OnReconcileFrames += ReconcileFrames;
    }

    protected override void NetworkStart () {
        base.NetworkStart();
        if (networkObject.IsServer) {
            _owningPlayer = NetworkManager.Instance.Networker.GetPlayerById(networkObject.ownerId);
        } else {
            networkObject.ownerIdChanged += NetworkObject_ownerIdChanged;
        }
    }

    void FixedUpdate () {
        if (networkObject == null) {
            return;
        }

        // Everyone that is not the server or the controlling player will just set the position.
        if (!networkObject.IsServer && !_isOwner) {
            transform.position = networkObject.position;
            return;
        }

        // We advance one frame in the simulation
        _listener.AdvanceFrame();

        // The controlling player saves this Frame with his last input and action recorded.
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
            CorrectError();
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

        // We record the players directional input.
        _listener.RecordMovement(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // Record any action the player might take!
        if (Input.GetKeyDown(KeyCode.Space)) {
            _listener.RecordAction(1, 0); // 1 = Jump;
        }

        // Press 'X' during simulation to test Server-Reconciliation!
        if (Input.GetKeyDown(KeyCode.X)) {
            transform.Translate((Vector3.forward + Vector3.left) * 3f);
        }
    }

    void OnCollisionEnter (Collision pCollision) {
        _body.useGravity = true;
        _isJumping = false;
    }

    #endregion

    #region InputListener-Events
    void SyncFrame () {
        // The player sends the frames he played to the server.
        if (_isOwner && _listener.FramesToSend.Count > 0) {
            networkObject.SendRpc(RPC_SYNC_INPUTS, Receivers.Server, _listener.DequeueFramesToSend());
        }

        // The server sends back what he played to the controlling player.
        if (networkObject.IsServer && _listener.LocalInputHistory.Count > 0) {
            networkObject.SendRpcUnreliable(_owningPlayer, RPC_SYNC_INPUT_HISTORY, _listener.DequeueLocalInputHistory());
        }
    }

    void PlayFrame (float pSpeed, InputFrame pInputFrame) {
        // Here we provide an implementation on how we want our GameObject to move on input.
        transform.position += MoveDelta(pSpeed, pInputFrame);
        if (!pInputFrame.HasActions) {
            return;
        }

        // Here we provide an implementation for the actions we recorded.
        for (int i = 0; i < pInputFrame.actions.Length; i++) {
            switch (pInputFrame.actions[i].actionId) {
                case 1:
                    transform.position += JumpDelta(pInputFrame.actions[i]);
                    break;
                default:
                    break;
            }
        }
    }

    void ReconcileFrames (float pDistance, InputFrameHistoryItem pLocalItem, InputFrameHistoryItem pServerItem, IEnumerable<InputFrameHistoryItem> pItemsToReconcile) {
        // Here we provide an implementation for the reconciling and replaying frames if anything went wrong.

        // We set our current position to the servers-position and simply replay every input we made and then we should end up where the server is.
        //transform.position = = new Vector3(pServerItem.xPosition, pServerItem.yPosition, pServerItem.zPosition);
        Vector3 serverPosition = new Vector3(pServerItem.xPosition, pServerItem.yPosition, pServerItem.zPosition);
        foreach (var item in pItemsToReconcile) {
            serverPosition += MoveDelta(_listener.Speed, item.inputFrame);
            if (!item.inputFrame.HasActions) {
                continue;
            }

            for (int i = 0; i < item.inputFrame.actions.Length; i++) {
                switch (item.inputFrame.actions[i].actionId) {
                    case 1:
                        serverPosition += JumpDelta(item.inputFrame.actions[i]);
                        break;
                    default:
                        break;
                }
            }

            //PerformMovement(_listener.Speed, item.inputFrame);
            //if (item.inputFrame.HasActions) {
            //    PerformAction(item.inputFrame);
            //}
        }

        _errorMargin = serverPosition - transform.position;
    }

    void CorrectError () {
        if (_errorMargin.sqrMagnitude > 0.006f) {
            Vector3 lerp = Vector3.Lerp(Vector3.zero, _errorMargin, 0.15f);
            _errorMargin -= lerp;
            transform.position += lerp;
        }
    }

    Vector3 MoveDelta (float pSpeed, InputFrame pInputFrame) {
        return Vector3.ClampMagnitude(new Vector3(pInputFrame.horizontalMovement, 0f, pInputFrame.verticalMovement) * pSpeed * Time.fixedDeltaTime, pSpeed);
    }

    Vector3 JumpDelta (ActionFrame pActionFrame) {
        byte step = pActionFrame.data[0];
        if (step == 0) {
            _body.useGravity = false;
            _isJumping = true;
        }

        if (step < 150 && _isJumping) {
            _listener.RecordAction(1, ++step);
            if (step < 50) {
                return Vector3.up * -Physics.gravity.y * Time.fixedDeltaTime;
            } else {
                return Vector3.up * Physics.gravity.y * Time.fixedDeltaTime;
            }
        } else {
            _body.useGravity = true;
            _isJumping = false;
        }

        return Vector3.zero;
    }

    #endregion

    #region Events
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
        List<InputFrameHistoryItem> serverItems = pArgs.GetNext<byte[]>().ByteArrayToObject<List<InputFrameHistoryItem>>();

        // The serverItems are being sent unreliably and can arrive out of order. We check if the serverItems have arrived too late.
        if (serverItems.Count > 0
            && _listener.AuthorativeInputHistory.Count > 0
            && serverItems[serverItems.Count - 1].frame <= _listener.AuthorativeInputHistory[_listener.AuthorativeInputHistory.Count - 1].frame) {
            return;
        }

        _listener.AddAuthoritativeInputHistory(serverItems);
    }

    #endregion
}


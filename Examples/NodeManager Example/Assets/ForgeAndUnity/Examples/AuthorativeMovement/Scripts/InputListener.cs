using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ForgeAndUnity.Forge;

[System.Serializable]
public class InputListener {
    //Fields
    [SerializeField] protected float _speed;
    [SerializeField] protected uint _currentFrame;
    [SerializeField] protected int _frameSyncRate;
    [SerializeField] protected float _reconcileDistance;
    protected InputFrame _nextInputFrame;
    protected List<byte> _nextActions;
    protected List<InputFrame> _framesToPlay;
    protected List<InputFrame> _framesToSend;
    protected List<InputFrameHistoryItem> _localInputHistory;
    protected List<InputFrameHistoryItem> _authorativeInputHistory;

    public float Speed { get { return _speed; } set { _speed = value; } }
    public uint CurrentFrame { get { return _currentFrame; } set { _currentFrame = value; } }
    public int FrameSyncRate { get { return _frameSyncRate; } set { _frameSyncRate = value; } }
    public float ReconcileDistance { get { return _reconcileDistance; } set { _reconcileDistance = value; } }
    public InputFrame NextInputFrame { get { return _nextInputFrame; } }
    public List<InputFrame> FramesToPlay { get { return _framesToPlay; } }
    public List<InputFrame> FramesToSend { get { return _framesToSend; } }
    public List<InputFrameHistoryItem> LocalInputHistory { get { return _localInputHistory; } }
    public List<InputFrameHistoryItem> AuthorativeInputHistory { get { return _authorativeInputHistory; } }

    //Events
    public delegate void SyncFrameEvent ();
    public event SyncFrameEvent OnSyncFrame;
    public delegate void PerformMovementEvent (float pSpeed, InputFrame pInputFrame);
    public event PerformMovementEvent OnPerformMovement;
    public delegate void PerformActionEvent (InputFrame pInputFrame);
    public event PerformActionEvent OnPerformAction;


    //Functions
    public InputListener () {
        _nextActions = new List<byte>();
        _framesToPlay = new List<InputFrame>();
        _framesToSend = new List<InputFrame>();
        _localInputHistory = new List<InputFrameHistoryItem>();
        _authorativeInputHistory = new List<InputFrameHistoryItem>();
    }

    public InputListener (float pMovementSpeed, int pFrameSyncRate) : this() {
        _speed = pMovementSpeed;
        _frameSyncRate = pFrameSyncRate;
    }

    public void RecordInput (float pHorizontalInput, float pVerticalInput) {
        _nextInputFrame.horizontalInput = pHorizontalInput;
        _nextInputFrame.verticalInput = pVerticalInput;
    }

    public void RecordAction (byte pActionId) {
        _nextActions.Add(pActionId);
    }

    public void AdvanceFrame () {
        _currentFrame++;
        _nextInputFrame.frame = _currentFrame;
    }

    public void SaveFrame () {
        if (_nextActions.Count > 0) {
            _nextInputFrame.actions = _nextActions.ToArray();
            _nextActions.Clear();
        }
        
        _framesToPlay.Add(_nextInputFrame);
        _framesToSend.Add(_nextInputFrame);
        _nextInputFrame.actions = null;
    }

    public void PlayFrame (Transform pTransform) {
        if (_framesToPlay.Count == 0) {
            return;
        }

        InputFrame frame = _framesToPlay[0];
        RaisePerformMovement(_speed, frame);
        if (frame.actions != null) {
            RaisePerformAction(frame);
        }

        _localInputHistory.Add(GetMovementHistoryItem(frame, pTransform.position.x, pTransform.position.y, pTransform.position.z));
        _framesToPlay.RemoveAt(0);
        if (_currentFrame % _frameSyncRate == 0) {
            RaiseSyncFrame();
        }
    }

    public void ReconcileFrames (Transform pTransform) {
        while (_authorativeInputHistory.Count > 0) {
            InputFrameHistoryItem serverItem = _authorativeInputHistory[0];
            int localItemIndex;
            InputFrameHistoryItem localItem = TryGetMatchingLocalInputHistory(serverItem.frame, out localItemIndex);
            if (localItemIndex < 0) {
                _authorativeInputHistory.RemoveAt(0);
                continue;
            }

            if (GetHistoryDistance(serverItem, localItem) > _reconcileDistance) {
                // New event for OnStartServerReconciliation
                pTransform.position = new Vector3(serverItem.xPosition, serverItem.yPosition, serverItem.zPosition);
                var itemsToReplay = _localInputHistory.Where(x => x.frame >= serverItem.frame);

                foreach (var inputItemToReconcile in itemsToReplay) {
                    // New Event for OnReconciliateFrame
                    RaisePerformMovement(_speed, inputItemToReconcile.inputFrame);
                    if (inputItemToReconcile.inputFrame.actions != null) {
                        RaisePerformAction(inputItemToReconcile.inputFrame);
                    }
                }

                _localInputHistory.RemoveAt(localItemIndex);
            }

            _authorativeInputHistory.RemoveAt(0);
        }
    }

    public byte[] DequeueFramesToSend () {
        byte[] data = _framesToSend.ObjectToByteArray();
        _framesToSend.Clear();
        return data;
    }

    public byte[] DequeueLocalInputHistory () {
        byte[] data = _localInputHistory.ObjectToByteArray();
        _localInputHistory.Clear();
        return data;
    }

    public void AddFramesToPlay (List<InputFrame> pFrames) {
        _framesToPlay.AddRange(pFrames);
    }

    public void AddAuthoritativeInputHistory (List<InputFrameHistoryItem> pHistory) {
        _authorativeInputHistory.AddRange(pHistory);
    }

    float GetHistoryDistance (InputFrameHistoryItem pServerItem, InputFrameHistoryItem pLocalItem) {
        var serverPosition = new Vector3(pServerItem.xPosition, pServerItem.yPosition, pServerItem.zPosition);
        var localPosition = new Vector3(pLocalItem.xPosition, pLocalItem.yPosition, pLocalItem.zPosition);
        return Vector3.Distance(localPosition, serverPosition);
    }

    InputFrameHistoryItem GetMovementHistoryItem (InputFrame pInputFrame, float pXPosition, float pYPosition, float pZPosition) {
        InputFrameHistoryItem movementHistoryItem = new InputFrameHistoryItem() {
            xPosition = pXPosition,
            yPosition = pYPosition,
            zPosition = pZPosition,
            frame = pInputFrame.frame,
            inputFrame = pInputFrame
        };

        return movementHistoryItem;
    }


    public InputFrameHistoryItem TryGetMatchingLocalInputHistory (uint pAuthorativeFrame, out int pIndex) {
        for (int i = 0; i < _localInputHistory.Count; i++) {
            if (_localInputHistory[i].frame == pAuthorativeFrame) {
                pIndex = i;
                return _localInputHistory[i];
            }
        }

        pIndex = -1;
        return default(InputFrameHistoryItem);
    }

    #region Events
    public void RaiseSyncFrame () {
        if (OnSyncFrame != null) {
            OnSyncFrame();
        }
    }

    public void RaisePerformMovement (float pSpeed, InputFrame pInputFrame) {
        if (OnPerformMovement != null) {
            OnPerformMovement(pSpeed, pInputFrame);
        }
    }

    public void RaisePerformAction (InputFrame pInputFrame) {
        if (OnPerformAction != null) {
            OnPerformAction(pInputFrame);
        }
    }

    #endregion
}


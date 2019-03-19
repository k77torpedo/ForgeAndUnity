using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ForgeAndUnity.Forge;

[System.Serializable]
public class InputListener {
    //Fields
    [SerializeField] protected float _speed;
    [SerializeField] protected int _frameSyncRate;
    [SerializeField] protected float _reconcileDistance;
    protected uint _currentFrame;
    protected InputFrame _currentInputFrame;
    protected Dictionary<byte, ActionFrame> _nextActions;
    protected List<InputFrame> _framesToPlay;
    protected List<InputFrame> _framesToSend;
    protected List<InputFrameHistoryItem> _localInputHistory;
    protected List<InputFrameHistoryItem> _authorativeInputHistory;

    public float Speed { get { return _speed; } set { _speed = value; } }
    public uint CurrentFrame { get { return _currentFrame; } set { _currentFrame = value; } }
    public int FrameSyncRate { get { return _frameSyncRate; } set { _frameSyncRate = value; } }
    public float ReconcileDistance { get { return _reconcileDistance; } set { _reconcileDistance = value; } }
    public InputFrame CurrentInputFrame { get { return _currentInputFrame; } }
    public List<InputFrame> FramesToPlay { get { return _framesToPlay; } }
    public List<InputFrame> FramesToSend { get { return _framesToSend; } }
    public List<InputFrameHistoryItem> LocalInputHistory { get { return _localInputHistory; } }
    public List<InputFrameHistoryItem> AuthorativeInputHistory { get { return _authorativeInputHistory; } }

    //Events
    public delegate void SyncFrameEvent ();
    public event SyncFrameEvent OnSyncFrame;
    public delegate void PlayFrameEvent (float pSpeed, InputFrame pFrame);
    public event PlayFrameEvent OnPlayFrame;
    public delegate void ReconcileFramesEvent (float pDistance, InputFrameHistoryItem pLocalItem, InputFrameHistoryItem pServerItem, IEnumerable<InputFrameHistoryItem> pItemsToReconcile);
    public event ReconcileFramesEvent OnReconcileFrames;


    //Functions
    public InputListener () {
        _nextActions = new Dictionary<byte, ActionFrame>();
        _framesToPlay = new List<InputFrame>();
        _framesToSend = new List<InputFrame>();
        _localInputHistory = new List<InputFrameHistoryItem>();
        _authorativeInputHistory = new List<InputFrameHistoryItem>();
    }

    public InputListener (float pMovementSpeed, int pFrameSyncRate) : this() {
        _speed = pMovementSpeed;
        _frameSyncRate = pFrameSyncRate;
    }

    public void RecordMovement (float pHorizontalInput, float pVerticalInput) {
        _currentInputFrame.horizontalMovement = pHorizontalInput;
        _currentInputFrame.verticalMovement = pVerticalInput;
    }

    public void RecordAction(byte pActionId, byte pData) {
        RecordAction(pActionId, new byte[] { pData });
    }

    public void RecordAction(byte pActionId, object pData) {
        RecordAction(pActionId, pData.ObjectToByteArray());
    }

    public void RecordAction(byte pActionId, byte[] pData = null) {
        _nextActions[pActionId] = new ActionFrame() {
            actionId = pActionId,
            data = pData
        };
    }

    public void AdvanceFrame () {
        _currentFrame++;
        _currentInputFrame.frame = _currentFrame;
    }

    public void SaveFrame () {
        if (_nextActions.Count > 0) {
            _currentInputFrame.actions = _nextActions.Values.ToArray();
            _nextActions.Clear();
        }

        _framesToPlay.Add(_currentInputFrame);
        _framesToSend.Add(_currentInputFrame);
        _currentInputFrame.actions = null;
    }

    public void PlayFrame (Transform pTransform) {
        if (_framesToPlay.Count == 0) {
            return;
        }

        InputFrame frame = _framesToPlay[0];
        RaisePlayFrame(_speed, frame);
        _localInputHistory.Add(GetMovementHistoryItem(frame, pTransform.position.x, pTransform.position.y, pTransform.position.z));
        _framesToPlay.RemoveAt(0);
        if (_currentFrame % _frameSyncRate == 0) {
            RaiseSyncFrame();
        }
    }

    public void ReconcileFrames () {
        while (_authorativeInputHistory.Count > 0) {
            InputFrameHistoryItem serverItem = _authorativeInputHistory[0];
            int localItemIndex;
            InputFrameHistoryItem localItem = TryGetMatchingLocalInputHistory(serverItem.frame, out localItemIndex);
            if (localItemIndex < 0) {
                _authorativeInputHistory.RemoveAt(0);
                continue;
            }

            float distance = GetHistoryDistance(serverItem, localItem);
            if (distance > _reconcileDistance) {
                var itemsToReconcile = _localInputHistory.Where(x => x.frame >= serverItem.frame);
                RaiseReconcileFrames(distance, localItem, serverItem, itemsToReconcile);
                _localInputHistory.RemoveAt(localItemIndex);//TODO remove localInputHistoryItems (how many + how far, all input before that?)
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

    InputFrameHistoryItem GetMovementHistoryItem (InputFrame pFrame, float pXPosition, float pYPosition, float pZPosition) {
        InputFrameHistoryItem movementHistoryItem = new InputFrameHistoryItem() {
            xPosition = pXPosition,
            yPosition = pYPosition,
            zPosition = pZPosition,
            frame = pFrame.frame,
            inputFrame = pFrame
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

    public void RaisePlayFrame (float pSpeed, InputFrame pFrame) {
        if (OnPlayFrame != null) {
            OnPlayFrame(pSpeed, pFrame);
        }
    }

    public void RaiseReconcileFrames (float pDistance, InputFrameHistoryItem pLocalItem, InputFrameHistoryItem pServerItem, IEnumerable<InputFrameHistoryItem> pItemsToReconcile) {
        if (OnReconcileFrames != null) {
            OnReconcileFrames(pDistance, pLocalItem, pServerItem, pItemsToReconcile);
        }
    }

    #endregion
}


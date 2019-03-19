using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ForgeAndUnity.Forge;

/// <summary>
/// Allows for the recording, saving and syncing of input with authorative reconciliation.
/// </summary>
[System.Serializable]
public class InputListener {
    //Fields
    [SerializeField] protected float                    _speed;
    [SerializeField] protected int                      _frameSyncRate;
    [SerializeField] protected float                    _reconcileDistance;
    protected uint                                      _currentFrame;
    protected uint                                      _authorativeFrame;
    protected InputFrame                                _currentInputFrame;
    protected Dictionary<byte, ActionFrame>             _currentActions;
    protected Queue<InputFrame>                         _framesToPlay;
    protected List<InputFrame>                          _framesToSend;
    protected List<InputFrameHistoryItem>               _localInputHistory;
    protected Queue<InputFrameHistoryItem>              _authorativeInputHistory;

    public float                                        Speed                               { get { return _speed; } set { _speed = value; } }
    public uint                                         CurrentFrame                        { get { return _currentFrame; } set { _currentFrame = value; } }
    public uint                                         AuthorativeFrame                    { get { return _authorativeFrame; } set { _authorativeFrame = value; } }
    public int                                          FrameSyncRate                       { get { return _frameSyncRate; } set { _frameSyncRate = value; } }
    public float                                        ReconcileDistance                   { get { return _reconcileDistance; } set { _reconcileDistance = value; } }
    public InputFrame                                   CurrentInputFrame                   { get { return _currentInputFrame; } }
    public Dictionary<byte, ActionFrame>                CurrentActions                      { get { return _currentActions; } }
    public Queue<InputFrame>                            FramesToPlay                        { get { return _framesToPlay; } }
    public List<InputFrame>                             FramesToSend                        { get { return _framesToSend; } }
    public List<InputFrameHistoryItem>                  LocalInputHistory                   { get { return _localInputHistory; } }
    public Queue<InputFrameHistoryItem>                 AuthorativeInputHistory             { get { return _authorativeInputHistory; } }

    //Events
    public delegate void SyncFrameEvent ();
    public event SyncFrameEvent OnSyncFrame;
    public delegate void PlayFrameEvent (float pSpeed, InputFrame pFrame);
    public event PlayFrameEvent OnPlayFrame;
    public delegate void ReconcileFramesEvent (float pDistance, InputFrameHistoryItem pLocalItem, InputFrameHistoryItem pServerItem, IEnumerable<InputFrameHistoryItem> pItemsToReconcile);
    public event ReconcileFramesEvent OnReconcileFrames;


    //Functions
    public InputListener () : this(1f, 1, 0.6f, 0) { }

    public InputListener (float pSpeed, int pFrameSyncRate, float pReconcileDistance, int pCapacity) {
        _speed = pSpeed;
        _frameSyncRate = pFrameSyncRate;
        _reconcileDistance = pReconcileDistance;
        _currentActions = new Dictionary<byte, ActionFrame>(pCapacity);
        _framesToPlay = new Queue<InputFrame>(pCapacity);
        _framesToSend = new List<InputFrame>(pCapacity);
        _localInputHistory = new List<InputFrameHistoryItem>(pCapacity);
        _authorativeInputHistory = new Queue<InputFrameHistoryItem>(pCapacity);
    }

    public virtual void RecordMovement (float pHorizontalMovement, float pVerticalMovement) {
        _currentInputFrame.horizontalMovement = pHorizontalMovement;
        _currentInputFrame.verticalMovement = pVerticalMovement;
    }

    public virtual void RecordAction(byte pActionId, byte pData) {
        RecordAction(pActionId, new byte[] { pData });
    }

    public virtual void RecordAction(byte pActionId, object pData) {
        RecordAction(pActionId, pData.ObjectToByteArray());
    }

    public virtual void RecordAction(byte pActionId, byte[] pData = null) {
        _currentActions[pActionId] = new ActionFrame() {
            actionId = pActionId,
            data = pData
        };
    }

    public virtual void AdvanceFrame () {
        _currentFrame++;
        _currentInputFrame.frame = _currentFrame;
    }

    public virtual void SaveFrame () {
        if (_currentActions.Count > 0) {
            _currentInputFrame.actions = _currentActions.Values.ToArray();
            _currentActions.Clear();
        }

        _framesToPlay.Enqueue(_currentInputFrame);
        _framesToSend.Add(_currentInputFrame);
        _currentInputFrame.actions = null;
    }

    public virtual void PlayFrame (Transform pTransform) {
        if (_framesToPlay.Count == 0) {
            return;
        }

        InputFrame frame = _framesToPlay.Dequeue();
        RaisePlayFrame(_speed, frame);
        _localInputHistory.Add(GetMovementHistoryItem(frame, pTransform.position.x, pTransform.position.y, pTransform.position.z));
        if (_currentFrame % _frameSyncRate == 0) {
            RaiseSyncFrame();
        }
    }

    public virtual void ReconcileFrames () {
        ClearLocalInputHistory(_authorativeFrame);
        while (_authorativeInputHistory.Count > 0) {
            InputFrameHistoryItem serverItem = _authorativeInputHistory.Dequeue();
            InputFrameHistoryItem localItem = FindLocalInputHistoryItemOrDefault(serverItem.inputFrame.frame);
            if (localItem.inputFrame.frame == 0) {
                continue;
            }

            float distance = GetHistoryDistance(serverItem, localItem);
            if (distance > _reconcileDistance) {
                var itemsToReconcile = _localInputHistory.Where(x => x.inputFrame.frame >= serverItem.inputFrame.frame);
                RaiseReconcileFrames(distance, localItem, serverItem, itemsToReconcile);
            }

            _authorativeFrame = serverItem.inputFrame.frame;
        }
    }

    public virtual void ClearLocalInputHistory (uint pUntilFrame) {
        int count = 0;
        for (int i = 0; i < _localInputHistory.Count; i++) {
            if (_localInputHistory[i].inputFrame.frame <= pUntilFrame) {
                count++;
            } else {
                break;
            }
        }

        _localInputHistory.RemoveRange(0, count);
    }

    public virtual byte[] DequeueFramesToSend () {
        byte[] data = _framesToSend.ToArray().ObjectToByteArray();
        _framesToSend.Clear();
        return data;
    }

    public virtual byte[] DequeueLocalInputHistory () {
        byte[] data = _localInputHistory.ToArray().ObjectToByteArray();
        _localInputHistory.Clear();
        return data;
    }

    public virtual void AddFramesToPlay (InputFrame[] pFrames) {
        for (int i = 0; i < pFrames.Length; i++) {
            _framesToPlay.Enqueue(pFrames[i]);
        }
    }

    public virtual void AddAuthoritativeInputHistory (InputFrameHistoryItem[] pHistory) {
        for (int i = 0; i < pHistory.Length; i++) {
            _authorativeInputHistory.Enqueue(pHistory[i]);
        }
    }

    public virtual float GetHistoryDistance (InputFrameHistoryItem pServerItem, InputFrameHistoryItem pLocalItem) {
        return Vector3.Distance(
            new Vector3(pLocalItem.xPosition, pLocalItem.yPosition, pLocalItem.zPosition),
            new Vector3(pServerItem.xPosition, pServerItem.yPosition, pServerItem.zPosition)
        );
    }

    public virtual InputFrameHistoryItem GetMovementHistoryItem (InputFrame pFrame, float pXPosition, float pYPosition, float pZPosition) {
        return new InputFrameHistoryItem() {
            xPosition = pXPosition,
            yPosition = pYPosition,
            zPosition = pZPosition,
            inputFrame = pFrame
        };
    }

    public virtual InputFrameHistoryItem FindLocalInputHistoryItemOrDefault (uint pAuthorativeFrame) {
        for (int i = 0; i < _localInputHistory.Count; i++) {
            if (_localInputHistory[i].inputFrame.frame == pAuthorativeFrame) {
                return _localInputHistory[i];
            }
        }

        return default(InputFrameHistoryItem);
    }

    public virtual void RaiseSyncFrame () {
        if (OnSyncFrame != null) {
            OnSyncFrame();
        }
    }

    public virtual void RaisePlayFrame (float pSpeed, InputFrame pFrame) {
        if (OnPlayFrame != null) {
            OnPlayFrame(pSpeed, pFrame);
        }
    }

    public virtual void RaiseReconcileFrames (float pDistance, InputFrameHistoryItem pLocalItem, InputFrameHistoryItem pServerItem, IEnumerable<InputFrameHistoryItem> pItemsToReconcile) {
        if (OnReconcileFrames != null) {
            OnReconcileFrames(pDistance, pLocalItem, pServerItem, pItemsToReconcile);
        }
    }
}


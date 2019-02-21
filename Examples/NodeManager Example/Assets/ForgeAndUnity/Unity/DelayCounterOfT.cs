using System;

/// <summary>
/// Lightweight class for managing delays using relative/incremental values.
/// </summary>
public abstract class DelayCounter<T> where T : struct {
    //Fields
    protected bool              _stopped;
    protected T                 _countingTime;
    protected T                 _delayTime;
    protected Func<T>           _updater;

    public T                    CountingTime            { get { return _countingTime; } set { _countingTime = value; } }
    public T                    DelayTime               { get { return _delayTime; } set { _delayTime = value; } }
    public Func<T>              Updater                 { get { return _updater; } set { _updater = value; } }
    public virtual bool         HasStopped              { get { return _stopped; } }
    public abstract bool        HasPassed               { get; }
	public abstract T           RemainingTime           { get; }
    

    //Functions
    public DelayCounter (Func<T> pUpdater) {
		_updater = pUpdater;
	}

	public virtual void Start () {
        _stopped = false;
        _countingTime = default (T);
	}

	public virtual void Start (T pDelayTime) {
        _stopped = false;
        _countingTime = default (T);
		_delayTime = pDelayTime;
	}

    public virtual void Stop () {
        _stopped = true;
    }

    public abstract bool Update (T pCountingTime);
	protected abstract void Update ();
}

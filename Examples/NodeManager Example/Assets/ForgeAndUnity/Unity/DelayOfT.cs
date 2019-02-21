using System;

/// <summary>
/// Lightweight class for managing delays using absolute/global values in Unity.
/// </summary>
public abstract class Delay<T> where T : struct {
    //Fields
    protected T                 _startTime;
    protected T                 _currentTime;
    protected T                 _delayTime;
	protected Func<T>           _updater;
    protected bool              _stopped;
    protected bool              _firstValidValueIsStartTime;

    public T                    StartTime               { get { return _startTime; } set { _startTime = value; } }
    public T                    CurrentTime             { get { return _currentTime; } set { _currentTime = value; } }
    public T                    DelayTime               { get { return _delayTime; } set { _delayTime = value; } }
    public Func<T>              Updater                 { get { return _updater; } set { _updater = value; } }
    public bool                 HasStopped              { get { return _stopped; } }
	public abstract bool        HasPassed               { get; }
    public abstract T           RemainingTime           { get; }
    public abstract T           PassedTime              { get; }
    protected abstract bool     IsFirstValidValue       { get; }


    //Functions
    public Delay (Func<T> pUpdater) : this (pUpdater, false) { }

    public Delay (Func<T> pUpdater, bool pFirstValidValueIsStartTime) {
		_updater = pUpdater;
        _firstValidValueIsStartTime = pFirstValidValueIsStartTime;
	}

	public virtual void Start () {
		_stopped = false;
		_startTime = _updater.Invoke();
		_currentTime = _startTime;
	}

	public virtual void Start (T pDelayTime) {
		_stopped = false;
		_startTime = _updater.Invoke();
		_currentTime = _startTime;
		_delayTime = pDelayTime;
	}

	public virtual void Start (T pStarttime, T pDelayTime) {
		_stopped = false;
		_startTime = pStarttime;
		_currentTime = _startTime;
		_delayTime = pDelayTime;
	}

	public virtual void Stop () {
		_stopped = true;
	}

	public virtual void Update (T pCurrentTime) {
		if (_stopped) {
			return;
		}

		_delayTime = pCurrentTime;
	}

	protected virtual void Update () {
		if (_updater == null || _stopped) {
			return;
		}

        _currentTime = _updater.Invoke();
        if (_firstValidValueIsStartTime && IsFirstValidValue) {
            _startTime = _currentTime;
        }
	}
}
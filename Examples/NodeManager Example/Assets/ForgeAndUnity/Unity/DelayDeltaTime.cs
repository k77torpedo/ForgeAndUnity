using System;

/// <summary>
/// Lightweight class for managing time using relative/incremental values in Unity.
/// </summary>
public class DelayDeltaTime : DelayCounter<float> {
	//Fields
	public override bool HasPassed {
		get { 
			Update ();
			return _countingTime >= _delayTime;
		}
	}

	public override float RemainingTime {
		get { 
			return _delayTime - _countingTime;
		}
	}


	//Functions
	public DelayDeltaTime (Func<float> pUpdater) : base(pUpdater) { }

	public override bool Update (float pCountingTime) {
        _countingTime += pCountingTime;
        return CountingTime >= DelayTime;
    }

	protected override void Update () {
		if (_updater == null) {
			return;
		}

		_countingTime += _updater.Invoke ();
	}
}

using System;

namespace ForgeAndUnity.Unity {

    /// <summary>
    /// Lightweight class for managing serverTime-delays using absolute/global values in Unity.
    /// </summary>
    public class DelayFixedTime : Delay<float> {
        //Fields
        public override bool HasPassed {
            get {
                Update();
                return _currentTime > _startTime + _delayTime;
            }
        }

        public override float RemainingTime {
            get {
                return _startTime + _delayTime - _currentTime;
            }
        }

        public override float PassedTime {
            get {
                return _currentTime - _startTime;
            }
        }

        protected override bool IsFirstValidValue {
            get {
                return _startTime == 0f && _currentTime > 0f;
            }
        }


        //Functions
        public DelayFixedTime (Func<float> pUpdater) : base(pUpdater) { }

        public DelayFixedTime (Func<float> pUpdater, bool pFirstValueIsStartTime) : base(pUpdater, pFirstValueIsStartTime) { }
    }
}
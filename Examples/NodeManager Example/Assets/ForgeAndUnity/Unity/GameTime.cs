using System;
using UnityEngine;

namespace ForgeAndUnity.Unity {

    /// <summary>
    /// Wrapper for Unity-<see cref="Time"/> variables.
    /// </summary>
    public static class GameTime {
        //Fields
        public static float fixedTime {
            get { return Time.fixedTime; }
        }

        public static float deltaTime {
            get { return Time.deltaTime; }
        }

        public static float fixedDeltaTime {
            get { return Time.fixedDeltaTime; }
        }


        //Functions
        public static Func<float> DeltaTimeUpdater () {
            return () => { return deltaTime; };
        }

        public static Func<float> FixedDeltaTimeUpdater () {
            return () => { return fixedDeltaTime; };
        }

        public static Func<float> FixedTimeUpdater () {
            return () => { return fixedTime; };
        }
    }
}

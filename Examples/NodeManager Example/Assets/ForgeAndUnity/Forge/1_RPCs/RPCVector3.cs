using UnityEngine;

namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Lightweight container for serializing and deserializing <see cref="Vector3"/> over RPCs.
    /// </summary>
    [System.Serializable]
    public struct RPCVector3 {
        //Fields
        public static RPCVector3 zero = new RPCVector3() { x = 0f, y = 0f, z = 0f };
        public static RPCVector3 one = new RPCVector3() { x = 1f, y = 1f, z = 1f };
        public static RPCVector3 forward = new RPCVector3() { x = 0f, y = 0f, z = 1f };
        public static RPCVector3 back = new RPCVector3() { x = 0f, y = 0f, z = -1f };
        public static RPCVector3 right = new RPCVector3() { x = 1f, y = 0f, z = 0f };
        public static RPCVector3 left = new RPCVector3() { x = -1f, y = 0f, z = 0f };
        public static RPCVector3 up = new RPCVector3() { x = 0f, y = 1f, z = 0f };
        public static RPCVector3 down = new RPCVector3() { x = 0f, y = -1f, z = 0f };

        public float x;
        public float y;
        public float z;


        //Functions
        public static bool operator == (RPCVector3 a, RPCVector3 b) {
            return (a.x == b.x && a.y == b.y && a.z == b.z);
        }

        public static bool operator != (RPCVector3 a, RPCVector3 b) {
            return (a.x != b.x || a.y != b.y || a.z != b.z);
        }

        public Vector3 ToVector3 () {
            return new Vector3(x, y, z);
        }
    }
}

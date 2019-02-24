using System;
using UnityEngine;

namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Lightweight container for serializing and deserializing <see cref="Quaternion"/> over RPCs.
    /// </summary>
    [Serializable]
    public struct RPCQuaternion : IEquatable<RPCQuaternion> {
        //Fields
        public float x;
        public float y;
        public float z;
        public float w;


        //Functions
        public static bool operator == (RPCQuaternion a, RPCQuaternion b) {
            return (a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w);
        }

        public static bool operator != (RPCQuaternion a, RPCQuaternion b) {
            return (a.x != b.x || a.y != b.y || a.z != b.z || a.w != b.w);
        }

        public Quaternion ToQuaternion () {
            return new Quaternion(x, y, z, w);
        }

        public override bool Equals (object pObj) {
            return pObj is RPCQuaternion && Equals((RPCQuaternion)pObj);
        }

        public bool Equals (RPCQuaternion pOther) {
            return pOther == this;
        }

        public override int GetHashCode () {
            var hashCode = -1743314642;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            hashCode = hashCode * -1521134295 + w.GetHashCode();
            return hashCode;
        }
    }
}

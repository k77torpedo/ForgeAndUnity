using UnityEngine;

/// <summary>
/// Lightweight container for serializing and deserializing <see cref="Quaternion"/> over RPCs.
/// </summary>
[System.Serializable]
public struct RPCQuaternion {
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
}

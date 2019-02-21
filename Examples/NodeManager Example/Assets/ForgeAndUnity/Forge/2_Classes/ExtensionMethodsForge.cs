using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using BeardedManStudios.Forge.Networking.Unity;
using UnityEngine;

/// <summary>
/// Extension-methods for Forge Networking Remastered.
/// </summary>
public static class ExtensionMethodsForge {
    //Functions
	public static byte[] ObjectToByteArray (this object obj) {
		if (obj == null) {
			return null;
		}

		BinaryFormatter bf = new BinaryFormatter ();
		MemoryStream ms = new MemoryStream ();
		bf.Serialize (ms, obj);

		return ms.ToArray ();
	}

	public static T ByteArrayToObject<T>(this byte[] arrBytes) {
		MemoryStream ms = new MemoryStream ();
		BinaryFormatter bf = new BinaryFormatter ();
		ms.Write (arrBytes, 0, arrBytes.Length);
		ms.Seek (0, SeekOrigin.Begin);
        T obj = (T)bf.Deserialize(ms);

		return obj;
	}

	public static bool TryGetByteArrayToObject<T> (this byte[] arrBytes, out T obj) {
        MemoryStream ms = new MemoryStream();
        BinaryFormatter bf = new BinaryFormatter();
        ms.Write(arrBytes, 0, arrBytes.Length);
        ms.Seek(0, SeekOrigin.Begin);
        try {
            obj = (T)bf.Deserialize(ms);
            return true;
        } catch {
            obj = default(T);
            return false;
        }
    }

    public static RPCVector3 ToRPC(this Vector3 vector) {
		return new RPCVector3 () { x = vector.x, y = vector.y, z = vector.z };
	}

    public static RPCQuaternion ToRPC (this Quaternion quaternion) {
        return new RPCQuaternion() { x = quaternion.x, y = quaternion.y, z = quaternion.z, w = quaternion.w };
    }

    public static bool HasNetworkBehavior (this GameObject pGameObject) {
        return (pGameObject.GetComponent<NetworkBehavior>() != null);
    }
}

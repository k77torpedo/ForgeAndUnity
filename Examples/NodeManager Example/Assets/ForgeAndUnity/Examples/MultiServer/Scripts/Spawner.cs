using UnityEngine;
using BeardedManStudios.Forge.Networking.Unity;
using ForgeAndUnity.Forge;

/// <summary>
/// Creates <see cref="NetworkBehavior"/>s.
/// </summary>
public class Spawner : MonoBehaviour {
    //Fields
    public int createCode;
    public int amount;


    //Functions
    public void Start () {
        if (!NodeManager.IsInitialized || !NodeManager.Instance.IsServer) {
            return;
        }

        for (int i = 0; i < amount; i++) {
            NodeManager.Instance.InstantiateInScene(gameObject.scene.name, createCode, null, transform.position, transform.rotation);
        }
    }
}

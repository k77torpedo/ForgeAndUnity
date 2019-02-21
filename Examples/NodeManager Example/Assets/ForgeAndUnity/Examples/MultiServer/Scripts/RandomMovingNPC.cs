using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking;

/// <summary>
/// It randomly moves!!!!
/// </summary>
[RequireComponent(typeof(MeshRenderer), typeof(NavMeshAgent))]
public class RandomMovingNPC : RandomMovingNPCBehavior, INetworkSceneObject, IRPCSerializable {
    //Fields
    MeshRenderer _meshRenderer;
    NavMeshAgent _agent;

    public NetworkSceneManager Manager { get; set; }


    // Functions
    #region Unity
    void Awake() {
        _meshRenderer = GetComponent<MeshRenderer>();
        _agent = GetComponent<NavMeshAgent>();
        SetColor(new Color(Random.Range(0f, 255f), Random.Range(0f, 255f), Random.Range(0f, 255f)));
    }

    void Update() {
        if (networkObject == null) {
            return;
        }

        if (networkObject.IsServer) {
            networkObject.position = transform.position;
            networkObject.rotation = transform.rotation;
            InvokeRepeating("MoveRandom", 1f, 5f);
        } else {
            transform.position = networkObject.position;
            transform.rotation = networkObject.rotation;
        }
    }

    #endregion

    #region Helpers
    public void SetColor (Color pColor) {
        if (_meshRenderer == null || pColor == null) {
            return;
        }

        _meshRenderer.material.color = pColor;
    }

    public void MoveRandom () {
        _agent.Move(new Vector3(Random.Range(1f, 10f), 0f, Random.Range(1f, 10f)));
    }

    #endregion

    #region INetworkSceneObject-Implementation
    public void SetNetworkObject (NetworkObject pNetworkObject) {
        networkObject = pNetworkObject as RandomMovingNPCNetworkObject;
    }

    public NetworkObject GetNetworkObject () {
        return networkObject;
    }

    public uint GetNetworkId () {
        return (networkObject != null) ? networkObject.NetworkId : 0;
    }

    #endregion

    #region IRPCSerializable-Implementation
    public byte[] ToByteArray () {
        Color color = _meshRenderer.material.color;
        return new byte[] { (byte)color.r, (byte)color.g, (byte)color.b };
    }

    public void FromByteArray (byte[] pByteArray) {
        Color color = new Color(pByteArray[0], pByteArray[1], pByteArray[2]);
        _meshRenderer.material.color = color;
    }

    #endregion
}

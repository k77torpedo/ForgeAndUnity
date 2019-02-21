using UnityEngine;

/// <summary>
/// Describes static <see cref="NodeMap"/>-data that does not change during runtime like id, name, description or connection-information. 
/// Used by a <see cref="NodeManager"/>.
/// </summary>
[CreateAssetMenu(menuName = "ForgeAndHelpers/NodeMapSO")]
public class NodeMapSO : ScriptableObject {
    //Fields
    public NodeMap nodeMap;
}

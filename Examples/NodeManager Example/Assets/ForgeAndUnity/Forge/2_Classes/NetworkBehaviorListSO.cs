using UnityEngine;

/// <summary>
/// Describes static <see cref="NetworkBehaviorListItem"/>-related data that does not change during runtime like id, name or description. 
/// Used by a <see cref="NetworkSceneManager"/>.
/// </summary>
[CreateAssetMenu(menuName = "ForgeAndHelpers/NetworkBehaviorListSO")]
public class NetworkBehaviorListSO : ScriptableObject {
    //Fields
    public int id;
    public string behaviorSetName;
    public string description;
    public NetworkBehaviorList behaviorList;
}

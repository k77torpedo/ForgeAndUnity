using UnityEngine;

namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Describes static <see cref="NetworkBehaviorListItem"/>-related data that does not change during runtime like id, name or description. 
    /// Used by a <see cref="NetworkSceneManager"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "ForgeAndUnity/NetworkBehaviorListSO")]
    public class NetworkBehaviorListSO : ScriptableObject {
        //Fields
        public int id;
        public string behaviorSetName;
        public string description;
        public NetworkBehaviorList behaviorList;
    }
}

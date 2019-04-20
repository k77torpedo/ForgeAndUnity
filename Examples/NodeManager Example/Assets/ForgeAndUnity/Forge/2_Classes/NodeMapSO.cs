using UnityEngine;

namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Describes static <see cref="NodeMap"/>-data that does not change during runtime like id, name, description or connection-information. 
    /// Used by a <see cref="NodeManager"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "ForgeAndUnity/NodeMapSO")]
    public class NodeMapSO : ScriptableObject {
        //Fields
        public NodeMap nodeMap;
    }
}

using UnityEngine;

namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Used as an easy lookup for a <see cref="BeardedManStudios.Forge.Networking.Unity.NetworkBehavior"/> from a <see cref="NetworkBehaviorListSO"/>.
    /// </summary>
    [System.Serializable]
    public class NetworkBehaviorListItem {
        //Fields
        [SerializeField] int                    _createCode;
        [SerializeField] string                 _name;
        [SerializeField] GameObject             _networkBehavior;

        public int                              CreateCode                      { get { return _createCode; } protected set { _createCode = value; } }
        public string                           Name                            { get { return _name; } protected set { _name = value; } }
        public GameObject                       NetworkBehavior                 { get { return _networkBehavior; } protected set { _networkBehavior = value; } }


        //Functions
        public NetworkBehaviorListItem (int pCreateCode, string pName, GameObject pNetworkBehavior) {
            _createCode = pCreateCode;
            _name = pName;
            _networkBehavior = pNetworkBehavior;
        }
    }
}

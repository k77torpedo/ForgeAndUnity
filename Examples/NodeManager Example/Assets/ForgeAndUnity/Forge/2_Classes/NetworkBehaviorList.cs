using System.Collections.Generic;
using UnityEngine;

namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Class for providing help with looking up <see cref="NetworkBehaviorListItem"/>s in a <see cref="NetworkBehaviorListSO"/>.
    /// </summary>
    [System.Serializable]
    public class NetworkBehaviorList {
        //Fields
        [SerializeField] protected NetworkBehaviorListItem[]            _initWithNetworkBehaviorListItems;
        protected Dictionary<int, NetworkBehaviorListItem>              _networkBehaviors;
        protected Dictionary<string, int>                               _nameToCreateCode;

        public NetworkBehaviorListItem[]                                InitWithNetworkBehaviorListItems            { get { return _initWithNetworkBehaviorListItems; } }
        public Dictionary<int, NetworkBehaviorListItem>                 NetworkBehaviors                            { get { return _networkBehaviors; } }


        //Functions
        public NetworkBehaviorList () {
            _networkBehaviors = new Dictionary<int, NetworkBehaviorListItem>();
            _nameToCreateCode = new Dictionary<string, int>();
        }

        public NetworkBehaviorList (NetworkBehaviorListItem[] pInitWithNetworkBehaviorListItems) : this() {
            Init(pInitWithNetworkBehaviorListItems);
        }

        #region Init
        public virtual void Init () {
            Init(_initWithNetworkBehaviorListItems);
        }

        public virtual void Init (NetworkBehaviorListItem[] pItems) {
            if (pItems == null) {
                return;
            }

            _networkBehaviors.Clear();
            _nameToCreateCode.Clear();
            for (int i = 0; i < pItems.Length; i++) {
                if (pItems[i] == null ||
                    pItems[i].NetworkBehavior == null ||
                    _networkBehaviors.ContainsKey(pItems[i].CreateCode) ||
                    _nameToCreateCode.ContainsKey(pItems[i].Name)) {
                    continue;
                }

                _networkBehaviors.Add(pItems[i].CreateCode, pItems[i]);
                _nameToCreateCode.Add(pItems[i].Name.ToLower(), pItems[i].CreateCode);
            }
        }

        #endregion

        #region Helpers
        public virtual GameObject GetByName (string pName) {
            int createCode;
            if (!_nameToCreateCode.TryGetValue(pName, out createCode)) {
                return null;
            }

            return GetByCreateCode(createCode);
        }

        public virtual GameObject GetByCreateCode (int pCreateCode) {
            NetworkBehaviorListItem item;
            if (!_networkBehaviors.TryGetValue(pCreateCode, out item)) {
                return null;
            }

            return item.NetworkBehavior;
        }

        public virtual int GetCreateCodeFromName (string pName) {
            int createCode;
            if (!_nameToCreateCode.TryGetValue((pName ?? string.Empty).ToLower(), out createCode)) {
                return -1;
            }

            return createCode;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;

namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Lightweight container for serializing and deserializing <see cref="NetworkSceneManagerEndpoint"/> over RPCs.
    /// </summary>
    [Serializable]
    public struct RPCNetworkSceneManagerEndpoint : IEquatable<RPCNetworkSceneManagerEndpoint> {
        //Fields
        public string ip;
        public ushort port;
        

        //Functions
        public static bool operator == (RPCNetworkSceneManagerEndpoint a, RPCNetworkSceneManagerEndpoint b) {
            return (a.ip == b.ip && a.port == b.port);
        }

        public static bool operator != (RPCNetworkSceneManagerEndpoint a, RPCNetworkSceneManagerEndpoint b) {
            return (a.ip != b.ip || a.port != b.port);
        }

        public override bool Equals (object pObj) {
            return pObj is RPCNetworkSceneManagerEndpoint && Equals((RPCNetworkSceneManagerEndpoint)pObj);
        }

        public bool Equals (RPCNetworkSceneManagerEndpoint pOther) {
            return pOther == this;
        }

        public override int GetHashCode () {
            var hashCode = -1972451486;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ip);
            hashCode = hashCode * -1521134295 + port.GetHashCode();
            return hashCode;
        }
    }
}

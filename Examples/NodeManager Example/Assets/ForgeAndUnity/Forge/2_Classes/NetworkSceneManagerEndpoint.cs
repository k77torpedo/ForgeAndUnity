using UnityEngine;

namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Endpoint to connect or bind to with a <see cref="NetworkSceneManager"/>.
    /// </summary>
    [System.Serializable]
    public class NetworkSceneManagerEndpoint : IRPCSerializable<RPCNetworkSceneManagerEndpoint> {
        //Fields
        [SerializeField] string             _ip;
        [SerializeField] ushort             _port;

        public string                       Ip                  { get { return _ip; } set { _ip = value; } }
        public ushort                       Port                { get { return _port; } set { _port = value; } }


        //Functions
        public NetworkSceneManagerEndpoint () { }

        public NetworkSceneManagerEndpoint (NetworkSceneManagerEndpoint pEndpoint) {
            _ip = pEndpoint.Ip;
            _port = pEndpoint.Port;
        }

        public NetworkSceneManagerEndpoint (RPCNetworkSceneManagerEndpoint pEndpointRPC) {
            FromRPC(pEndpointRPC);
        }

        public NetworkSceneManagerEndpoint (string pHost, ushort pPort) {
            _ip = pHost;
            _port = pPort;
        }

        #region Serialization 
        public virtual RPCNetworkSceneManagerEndpoint ToRPC () {
            return new RPCNetworkSceneManagerEndpoint() {
                ip = _ip,
                port = _port
            };
        }

        public virtual void FromRPC (RPCNetworkSceneManagerEndpoint pNetworkSceneManagerEndpointRPC) {
            _ip = pNetworkSceneManagerEndpointRPC.ip;
            _port = pNetworkSceneManagerEndpointRPC.port;
        }

        public virtual byte[] ToByteArray () {
            return ToRPC().ObjectToByteArray();
        }

        public virtual void FromByteArray (byte[] pByteArray) {
            FromRPC(pByteArray.ByteArrayToObject<RPCNetworkSceneManagerEndpoint>());
        }

        #endregion
    }
}

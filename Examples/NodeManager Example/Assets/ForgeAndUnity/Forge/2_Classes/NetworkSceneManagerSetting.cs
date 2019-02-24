using UnityEngine;

namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Settings for setting up a <see cref="NetworkSceneManager"/> as a client or server.
    /// </summary>
    [System.Serializable]
    public class NetworkSceneManagerSetting : IRPCSerializable<RPCNetworkSceneManagerSetting> {
        //Fields
        [SerializeField] protected int                                  _maxConnections;
        [SerializeField] protected bool                                 _useTCP;
        [SerializeField] protected bool                                 _useMainThreadManagerForRPCs;
        [SerializeField] protected NetworkSceneManagerEndpoint          _serverAddress;
        [SerializeField] protected NetworkSceneManagerEndpoint          _serverNATAddress;
        [SerializeField] protected NetworkSceneManagerEndpoint          _clientAddress;
        [SerializeField] protected NetworkSceneManagerEndpoint          _clientNATAddress;

        public int                                                      MaxConnections                      { get { return _maxConnections; } set { _maxConnections = value; } }
        public bool                                                     UseTCP                              { get { return _useTCP; } set { _useTCP = value; } }
        public bool                                                     UseMainThreadManagerForRPCs         { get { return _useMainThreadManagerForRPCs; } set { _useMainThreadManagerForRPCs = value; } }
        public NetworkSceneManagerEndpoint                              ServerAddress                       { get { return _serverAddress; } set { _serverAddress = value; } }
        public NetworkSceneManagerEndpoint                              ServerNATAddress                    { get { return _serverNATAddress; } set { _serverNATAddress = value; } }
        public NetworkSceneManagerEndpoint                              ClientAddress                       { get { return _clientAddress; } set { _clientAddress = value; } }
        public NetworkSceneManagerEndpoint                              ClientNATAddress                    { get { return _clientNATAddress; } set { _clientNATAddress = value; } }


        //Functions
        public NetworkSceneManagerSetting () {
            _serverAddress = new NetworkSceneManagerEndpoint();
            _serverNATAddress = new NetworkSceneManagerEndpoint();
            _clientAddress = new NetworkSceneManagerEndpoint();
            _clientNATAddress = new NetworkSceneManagerEndpoint();
        }

        public NetworkSceneManagerSetting (NetworkSceneManagerSetting pSetting) {
            _maxConnections = pSetting.MaxConnections;
            _useTCP = pSetting.UseTCP;
            _useMainThreadManagerForRPCs = pSetting.UseMainThreadManagerForRPCs;
            _serverAddress = new NetworkSceneManagerEndpoint(pSetting.ServerAddress);
            _serverNATAddress = new NetworkSceneManagerEndpoint(pSetting.ServerNATAddress);
            _clientAddress = new NetworkSceneManagerEndpoint(pSetting.ClientAddress);
            _clientNATAddress = new NetworkSceneManagerEndpoint(pSetting.ClientNATAddress);
        }

        public NetworkSceneManagerSetting (RPCNetworkSceneManagerSetting pSettingRPC) : this() {
            FromRPC(pSettingRPC);
        }

        #region Serialization
        public virtual RPCNetworkSceneManagerSetting ToRPC () {
            return new RPCNetworkSceneManagerSetting() {
                maxConnections = _maxConnections,
                useTCP = _useTCP,
                useMainThreadManagerForRPCs = _useMainThreadManagerForRPCs,
                hostAddressRPC = ((_serverAddress != null) ? _serverAddress.ToRPC() : new RPCNetworkSceneManagerEndpoint()),
                hostNATAddressRPC = ((_serverNATAddress != null) ? _serverNATAddress.ToRPC() : new RPCNetworkSceneManagerEndpoint()),
                clientAddressRPC = ((_clientAddress != null) ? _clientAddress.ToRPC() : new RPCNetworkSceneManagerEndpoint()),
                clientNATAddressRPC = ((_clientNATAddress != null) ? _clientNATAddress.ToRPC() : new RPCNetworkSceneManagerEndpoint())
            };
        }

        public virtual void FromRPC (RPCNetworkSceneManagerSetting pNetworkSceneManagerSettingsRPC) {
            _maxConnections = pNetworkSceneManagerSettingsRPC.maxConnections;
            _useTCP = pNetworkSceneManagerSettingsRPC.useTCP;
            _useMainThreadManagerForRPCs = pNetworkSceneManagerSettingsRPC.useMainThreadManagerForRPCs;
            _serverAddress.FromRPC(pNetworkSceneManagerSettingsRPC.hostAddressRPC);
            _serverNATAddress.FromRPC(pNetworkSceneManagerSettingsRPC.hostNATAddressRPC);
            _clientAddress.FromRPC(pNetworkSceneManagerSettingsRPC.clientAddressRPC);
            _clientNATAddress.FromRPC(pNetworkSceneManagerSettingsRPC.clientNATAddressRPC);
        }

        public virtual byte[] ToByteArray () {
            return ToRPC().ObjectToByteArray();
        }

        public virtual void FromByteArray (byte[] pByteArray) {
            FromRPC(pByteArray.ByteArrayToObject<RPCNetworkSceneManagerSetting>());
        }

        #endregion
    }
}
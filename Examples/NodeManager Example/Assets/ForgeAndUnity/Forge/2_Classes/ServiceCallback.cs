namespace ForgeAndUnity.Forge {

    /// <summary>
    /// A callback for making a request through a service like <see cref="NodeService"/>. 
    /// <see cref="OnResponse"/> will contain the requests result and/or answer.
    /// </summary>
    public class ServiceCallback : IRPCSerializable<RPCServiceCallback> {
        //Fields
        protected ServiceCallbackStateEnum      _state;
        protected uint                          _callbackId;
        protected uint                          _sourceNodeId;
        protected float                         _requestTime;
        protected byte[]                        _requestData;
        protected float                         _responseTime;
        protected byte[]                        _responseData;

        public ServiceCallbackStateEnum         State                       { get { return _state; } set { _state = value; } }
        public uint                             CallbackId                  { get { return _callbackId; } set { _callbackId = value; } }
        public uint                             SourceNodeId                { get { return _sourceNodeId; } set { _sourceNodeId = value; } }
        public float                            RequestTime                 { get { return _requestTime; } set { _requestTime = value; } }
        public byte[]                           RequestData                 { get { return _requestData; } set { _requestData = value; } }
        public float                            ResponseTime                { get { return _responseTime; } set { _responseTime = value; } }
        public byte[]                           ResponseData                { get { return _responseData; } set { _responseData = value; } }

        //Events
        public delegate void ResponseEvent (float pResponseTime, byte[] pResponseData, ServiceCallback pSender);
        public event ResponseEvent OnResponse;
        public delegate void TimeoutEvent (ServiceCallback pSender);
        public event TimeoutEvent OnTimeout;

        //Functions
        public ServiceCallback () { }

        public ServiceCallback (uint pCallbackId, uint pSourceNodeId, byte[] pRequestData) {
            _callbackId = pCallbackId;
            _sourceNodeId = pSourceNodeId;
            _requestData = pRequestData;
        }

        public ServiceCallback (uint pCallbackId, uint pSourceNodeId, byte[] pRequestData, float pRequestTime)
            : this(pCallbackId, pSourceNodeId, pRequestData) {
            _requestTime = pRequestTime;
        }

        #region Events
        public virtual void RaiseResponse (float pResponseTime, byte[] pResponseData) {
            _responseTime = pResponseTime;
            _responseData = pResponseData;
            if (OnResponse != null) {
                OnResponse(pResponseTime, pResponseData, this);
            }
        }

        public virtual void RaiseTimeout () {
            if (OnTimeout != null) {
                OnTimeout(this);
            }
        }

        #endregion

        #region Serialization
        public virtual void FromRPC (RPCServiceCallback pServiceCallback) {
            _callbackId = pServiceCallback.callbackId;
            _sourceNodeId = pServiceCallback.sourceNodeId;
            _state = pServiceCallback.state;
            _requestData = pServiceCallback.data;
        }

        public virtual RPCServiceCallback ToRPC () {
            return new RPCServiceCallback() {
                callbackId = _callbackId,
                sourceNodeId = _sourceNodeId,
                state = _state,
                data = _requestData
            };
        }

        public virtual byte[] ToByteArray () {
            return ToRPC().ObjectToByteArray();
        }

        public virtual void FromByteArray (byte[] pByteArray) {
            FromRPC(pByteArray.ByteArrayToObject<RPCServiceCallback>());
        }

        #endregion
    }
}

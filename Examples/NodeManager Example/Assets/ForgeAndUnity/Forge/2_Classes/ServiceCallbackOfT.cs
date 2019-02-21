/// <summary>
/// A specialized version of <see cref="ServiceCallback"/>. Automatically deserializes the received data to <see cref="TResponseData"/>.
/// </summary>
public class ServiceCallback<TResponseData> : ServiceCallback {
    //Fields
    protected TResponseData _responseDataOfT;

    public TResponseData ResponseDataOfT {
        get { return _responseDataOfT; }
        set {
            _responseDataOfT = value;
            _responseData = (value != null) ? value.ObjectToByteArray() : null;
        }
    }

    //Events
    public delegate void ResponseOfTEvent (float pResponseTime, TResponseData pResponseDataOfT, ServiceCallback<TResponseData> pSender);
    public event ResponseOfTEvent OnResponseOfT;


    //Functions
    public ServiceCallback () { }

    public ServiceCallback (uint pCallbackId, uint pSourceNodeId) : this (pCallbackId, pSourceNodeId, 0f) { }

    public ServiceCallback (uint pCallbackId, uint pSourceNodeId, float pRequestTime) : base (pCallbackId, pSourceNodeId, null, pRequestTime) { }

    #region Events
    public override void RaiseResponse (float pResponseTime, byte[] pResponseData) {
        _responseDataOfT = pResponseData.ByteArrayToObject<TResponseData>();
        base.RaiseResponse(pResponseTime, pResponseData);
        if (OnResponseOfT != null) {
            OnResponseOfT(pResponseTime, _responseDataOfT, this);
        }
    }

    public virtual void RaiseResponseOfT (float pResponseTime, TResponseData pResponseData) {
        _responseDataOfT = pResponseData;
        base.RaiseResponse(pResponseTime, pResponseData.ObjectToByteArray());
        if (OnResponseOfT != null) {
            OnResponseOfT(pResponseTime, pResponseData, this);
        }
    }

    #endregion
}


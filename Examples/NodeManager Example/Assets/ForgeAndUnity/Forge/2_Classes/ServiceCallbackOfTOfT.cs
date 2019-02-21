/// <summary>
/// A specialized version of <see cref="ServiceCallback"/>. Additionally deserializes the sent data to <see cref="TRequestData"/>.
/// </summary>
public class ServiceCallback<TRequestData, TResponseData> : ServiceCallback<TResponseData>
{
    //Fields
    protected TRequestData _requestDataOfT;

    public TRequestData RequestDataOfT
    {
        get { return _requestDataOfT; }
        set {
            _requestDataOfT = value;
            _requestData = (value != null) ? value.ObjectToByteArray() : null;
        }
    }


    //Functions
    public ServiceCallback () { }

    public ServiceCallback (uint pCallbackId, uint pSourceNodeId, float pRequestTime)
        : this(pCallbackId, pSourceNodeId, default(TRequestData), pRequestTime) { }

    public ServiceCallback (uint pCallbackId, uint pSourceNodeId, TRequestData pRequestDataOfT)
        : this(pCallbackId, pSourceNodeId, pRequestDataOfT, 0f) { }

    public ServiceCallback (uint pCallbackId, uint pSourceNodeId, TRequestData pRequestData, float pRequestTime)
        : base(pCallbackId, pSourceNodeId, pRequestTime) {
        if (pRequestData != null) {
            base._requestData = pRequestData.ObjectToByteArray();
            _requestDataOfT = pRequestData;
        }
    }
}
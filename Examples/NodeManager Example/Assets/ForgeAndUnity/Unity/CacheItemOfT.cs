
/// <summary>
/// An item in a <see cref="CacheList{TCacheValue, TTimeStamp}"/> or a <see cref="CacheDictionary{TCacheKey, TCacheValue, TTimeStamp}"/>.
/// </summary>
/// <typeparam name="TValue">The object-type to be stored by the cache.</typeparam>
/// <typeparam name="TTimeStamp">The time-unit used (for example: <see cref="float"/> for seconds or <see cref="ulong"/> for milliseconds).</typeparam>
public class CacheItem<TValue, TTimeStamp> where TTimeStamp : struct {
    //Fields
    protected bool _isExpired;
    protected TValue _value;
    protected TTimeStamp _timeStamp;

    public bool IsExpired { get { return _isExpired; } set { _isExpired = value; } }
    public TValue Value { get { return _value; } set { _value = value; } }
    public TTimeStamp TimeStamp { get { return _timeStamp; } set { _timeStamp = value; } }


    //Functions
    public CacheItem (TValue pValue, TTimeStamp pTimeStamp) {
        _value = pValue;
        _timeStamp = pTimeStamp;
    }
}

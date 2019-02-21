using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Acts as a cache for any object. Be aware that <see cref="Update"/> or <see cref="UpdateCoroutine"/> must be called to flag a <see cref="CacheItem{TValue, TTimeStamp}"/> as 'expired'.
/// </summary>
/// <typeparam name="TCacheValue">The object-type to be stored by the cache.</typeparam>
/// <typeparam name="TTimeStamp">The time-unit used (for example: <see cref="float"/> for seconds or <see cref="ulong"/> for milliseconds).</typeparam>
public class CacheList<TCacheValue, TTimeStamp> where TTimeStamp : struct {
    //Fields
    protected List<CacheItem<TCacheValue, TTimeStamp>> _cacheItems;
    protected Delay<TTimeStamp> _lifeTimer;
    protected TTimeStamp _cacheLifeTime;

    public List<CacheItem<TCacheValue, TTimeStamp>> CacheItems { get { return _cacheItems; } }
    public Delay<TTimeStamp> LifeTimer { get { return _lifeTimer; } }
    public TTimeStamp CacheLifeTime { get { return _cacheLifeTime; } }

    //Events
    public delegate void OnCacheItemExpiredEvent(int pIndex, CacheItem<TCacheValue, TTimeStamp> pItem);
    public event OnCacheItemExpiredEvent OnCacheItemExpired;
    public delegate void OnCacheItemRemovedEvent (int pIndex, CacheItem<TCacheValue, TTimeStamp> pItem);
    public event OnCacheItemRemovedEvent OnCacheItemRemoved;


    //Functions
    public CacheList (Delay<TTimeStamp> pLifeTimer, TTimeStamp pCacheLifeTime) {
        _cacheItems = new List<CacheItem<TCacheValue, TTimeStamp>>();
        _lifeTimer = pLifeTimer;
        _cacheLifeTime = pCacheLifeTime;
    }

    public virtual void Add (TCacheValue pValue) {
        AddItem(new CacheItem<TCacheValue, TTimeStamp>(pValue, _lifeTimer.Updater.Invoke()));
    }

    public virtual void AddItem (CacheItem<TCacheValue, TTimeStamp> pItem) {
        _cacheItems.Add(pItem);
    }

    public virtual void RemoveAt (int pIndex) {
        RaiseCacheItemRemoved(pIndex, _cacheItems[pIndex]);
        _cacheItems.RemoveAt(pIndex);
    }

    public virtual bool IsExpired (CacheItem<TCacheValue, TTimeStamp> pItem) {
        _lifeTimer.Start(pItem.TimeStamp, _cacheLifeTime);
        return _lifeTimer.HasPassed;
    }

    public virtual TCacheValue[] GetExpired () {
        List<TCacheValue> values = new List<TCacheValue>();
        for (int i = 0; i < _cacheItems.Count; i++) {
            if (_cacheItems[i].IsExpired) {
                values.Add(_cacheItems[i].Value);
            }
        }

        return values.ToArray();
    }

    public virtual CacheItem<TCacheValue, TTimeStamp>[] GetExpiredItems () {
        List<CacheItem<TCacheValue, TTimeStamp>> values = new List<CacheItem<TCacheValue, TTimeStamp>>();
        for (int i = 0; i < _cacheItems.Count; i++) {
            if (_cacheItems[i].IsExpired) {
                values.Add(_cacheItems[i]);
            }
        }

        return values.ToArray();
    }

    public virtual void Update () {
        for (int i = 0; i < _cacheItems.Count; i++) {
            if (!_cacheItems[i].IsExpired && IsExpired(_cacheItems[i])) {
                _cacheItems[i].IsExpired = true;
                RaiseCacheItemExpired(i, _cacheItems[i]);
            }
        }
    }

    public virtual IEnumerator UpdateCoroutine () {
        while (true) {
            if (_cacheItems.Count == 0) {
                yield return null;
                continue;
            }

            yield return null;
            for (int i = 0; i < _cacheItems.Count; i++) {
                if (!_cacheItems[i].IsExpired && IsExpired(_cacheItems[i])) {
                    _cacheItems[i].IsExpired = true;
                    RaiseCacheItemExpired(i, _cacheItems[i]);
                }

                yield return null;
            }
        }
    }

    public virtual int RemoveExpired () {
        int amountRemoved = 0;
        for (int i = _cacheItems.Count - 1; i >= 0 && i < _cacheItems.Count; i--) {
            if (_cacheItems[i].IsExpired) {
                RemoveAt(i);
                amountRemoved++;
            }
        }

        return amountRemoved;
    }

    public virtual IEnumerator RemoveExpiredCoroutine () {
        while (true) {
            if (_cacheItems.Count == 0) {
                yield return null;
                continue;
            }

            yield return null;
            for (int i = _cacheItems.Count - 1; i >= 0 && i < _cacheItems.Count; i--) {
                if (_cacheItems[i].IsExpired) {
                    RemoveAt(i);
                }

                yield return null;
            }
        }
    }

    public virtual void Clear () {
        _cacheItems.Clear();
    }

    protected virtual void RaiseCacheItemExpired (int pIndex, CacheItem<TCacheValue, TTimeStamp> pItem) {
        if (OnCacheItemExpired != null) {
            OnCacheItemExpired(pIndex, pItem);
        }
    }

    protected virtual void RaiseCacheItemRemoved (int pIndex, CacheItem<TCacheValue, TTimeStamp> pItem) {
        if (OnCacheItemRemoved != null) {
            OnCacheItemRemoved(pIndex, pItem);
        }
    }
}

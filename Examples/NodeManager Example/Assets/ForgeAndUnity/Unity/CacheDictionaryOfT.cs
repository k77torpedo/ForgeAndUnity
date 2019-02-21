using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Acts as a cache for any object. Be aware that <see cref="Update"/> or <see cref="UpdateCoroutine"/> must be called to flag a <see cref="CacheItem{TValue, TTimeStamp}"/> as 'expired'.
/// </summary>
/// <typeparam name="TCacheKey">Key of the cache. This will be used to add and access items in the cache.</typeparam>
/// <typeparam name="TCacheValue">The object-type to be stored by the cache.</typeparam>
/// <typeparam name="TTimeStamp">The time-unit used (for example: <see cref="float"/> for seconds or <see cref="ulong"/> for milliseconds).</typeparam>
public class CacheDictionary<TCacheKey, TCacheValue, TTimeStamp> where TTimeStamp : struct {
    //Fields
    protected Dictionary<TCacheKey, CacheItem<TCacheValue, TTimeStamp>> _cacheItems;
    protected Delay<TTimeStamp> _lifeTimer;
    protected TTimeStamp _cacheLifeTime;

    public Dictionary<TCacheKey, CacheItem<TCacheValue, TTimeStamp>> CacheItems { get { return _cacheItems; } }
    public Delay<TTimeStamp> LifeTimer { get { return _lifeTimer; } }
    public TTimeStamp CacheLifeTime { get { return _cacheLifeTime; } }

    //Events
    public delegate void OnCacheItemExpiredEvent (TCacheKey pKey, CacheItem<TCacheValue, TTimeStamp> pItem);
    public event OnCacheItemExpiredEvent OnCacheItemExpired;
    public delegate void OnCacheItemRemovedEvent (TCacheKey pKey, CacheItem<TCacheValue, TTimeStamp> pItem);
    public event OnCacheItemRemovedEvent OnCacheItemRemoved;


    //Functions
    public CacheDictionary (Delay<TTimeStamp> pLifeTimer, TTimeStamp pCacheLifeTime) {
        _cacheItems = new Dictionary<TCacheKey, CacheItem<TCacheValue, TTimeStamp>>();
        _lifeTimer = pLifeTimer;
        _cacheLifeTime = pCacheLifeTime;
    }

    public virtual bool Add (TCacheKey pKey, TCacheValue pValue) {
        return AddItem(pKey, new CacheItem<TCacheValue, TTimeStamp>(pValue, _lifeTimer.Updater.Invoke()));
    }

    public virtual bool AddItem (TCacheKey pKey, CacheItem<TCacheValue, TTimeStamp> pItem) {
        if (_cacheItems.ContainsKey(pKey)) {
            return false;
        }

        _cacheItems.Add(pKey, pItem);
        return true;
    }

    public virtual bool Remove (TCacheKey pKey) {
        CacheItem<TCacheValue, TTimeStamp> item;
        if (!_cacheItems.TryGetValue(pKey, out item)) {
            return false;
        }

        RaiseCacheItemRemoved(pKey, item);
        return _cacheItems.Remove(pKey);
    }

    public virtual bool ContainsKey (TCacheKey pKey) {
        return _cacheItems.ContainsKey(pKey);
    }

    public virtual TCacheValue Get (TCacheKey pKey) {
        CacheItem<TCacheValue, TTimeStamp> value;
        _cacheItems.TryGetValue(pKey, out value);
        if (value == null) {
            return default(TCacheValue);
        }

        return value.Value;
    }

    public virtual CacheItem<TCacheValue, TTimeStamp> GetItem (TCacheKey pKey) {
        CacheItem<TCacheValue, TTimeStamp> item;
        _cacheItems.TryGetValue(pKey, out item);
        return item;
    }

    public virtual bool TryGetValue (TCacheKey pKey, out TCacheValue pValue) {
        CacheItem<TCacheValue, TTimeStamp> item;
        _cacheItems.TryGetValue(pKey, out item);
        if (item == null) {
            pValue = default(TCacheValue);
            return false;
        }

        pValue = item.Value;
        return true;
    }

    public virtual bool TryGetItem (TCacheKey pKey, out CacheItem<TCacheValue, TTimeStamp> pItem) {
        return _cacheItems.TryGetValue(pKey, out pItem);
    }

    public virtual bool IsExpired(CacheItem<TCacheValue, TTimeStamp> pItem) {
        _lifeTimer.Start(pItem.TimeStamp, _cacheLifeTime);
        return _lifeTimer.HasPassed;
    }

    public virtual TCacheValue[] GetExpired () {
        List<TCacheValue> values = new List<TCacheValue>();
        foreach (var item in _cacheItems.Values) {
            if (item.IsExpired) {
                values.Add(item.Value);
            }
        }

        return values.ToArray();
    }

    public virtual CacheItem<TCacheValue, TTimeStamp>[] GetExpiredItems () {
        List<CacheItem<TCacheValue, TTimeStamp>> values = new List<CacheItem<TCacheValue, TTimeStamp>>();
        foreach (var item in _cacheItems.Values) {
            if (item.IsExpired) {
                values.Add(item);
            }
        }

        return values.ToArray();
    }

    public virtual void Update () {
        List<KeyValuePair<TCacheKey, CacheItem<TCacheValue, TTimeStamp>>> expiredItems = new List<KeyValuePair<TCacheKey, CacheItem<TCacheValue, TTimeStamp>>>();
        foreach (var item in _cacheItems) {
            if (!item.Value.IsExpired && IsExpired(item.Value)) {
                item.Value.IsExpired = true;
                expiredItems.Add(item);
            }
        }

        // Firing RaiseCacheItemExpired() during a foreach-loop will cause an error if a subscriber tries to change the collection so we do it after the iteration.
        for (int i = 0; i < expiredItems.Count; i++) {
            RaiseCacheItemExpired(expiredItems[i].Key, expiredItems[i].Value);
        }
    }

    public virtual IEnumerator UpdateCoroutine () {
        List<TCacheKey> tmpExpiredKeys;
        while (true) {
            if (_cacheItems.Count == 0) {
                yield return null;
                continue;
            }

            // A foreach-iteration in a coroutine would lock the current collection for the duration so we access it via the collection-key instead.
            tmpExpiredKeys = _cacheItems.Keys.ToList();
            yield return null;
            for (int i = 0; i < tmpExpiredKeys.Count; i++) {
                CacheItem<TCacheValue, TTimeStamp> cacheItem;
                if (_cacheItems.TryGetValue(tmpExpiredKeys[i], out cacheItem) && !cacheItem.IsExpired && IsExpired(cacheItem)) {
                    cacheItem.IsExpired = true;
                    RaiseCacheItemExpired(tmpExpiredKeys[i], cacheItem);
                }

                yield return null;
            }
        }
    }

    public virtual int RemoveExpired () {
        int amountRemoved = 0;
        List<TCacheKey> tmpExpiredKeys = _cacheItems.Keys.ToList();
        for (int i = 0; i < tmpExpiredKeys.Count; i++) {
            CacheItem<TCacheValue, TTimeStamp> cacheItem;
            if (_cacheItems.TryGetValue(tmpExpiredKeys[i], out cacheItem) && cacheItem.IsExpired) {
                Remove(tmpExpiredKeys[i]);
                amountRemoved++;
            }
        }

        return amountRemoved;
    }

    public virtual IEnumerator RemoveExpiredCoroutine () {
        List<TCacheKey> tmpExpiredKeys;
        while (true) {
            if (_cacheItems.Count == 0) {
                yield return null;
                continue;
            }

            // A foreach-iteration in a coroutine would lock the current collection for the duration so we access it via the collection-key instead.
            tmpExpiredKeys = _cacheItems.Keys.ToList();
            yield return null;
            for (int i = 0; i < tmpExpiredKeys.Count; i++) {
                CacheItem<TCacheValue, TTimeStamp> cacheItem;
                if (_cacheItems.TryGetValue(tmpExpiredKeys[i], out cacheItem) && cacheItem.IsExpired) {
                    Remove(tmpExpiredKeys[i]);
                }

                yield return null;
            }
        }
    }

    public virtual void Clear () {
        _cacheItems.Clear();
    }

    protected virtual void RaiseCacheItemExpired (TCacheKey pKey, CacheItem<TCacheValue, TTimeStamp> pItem) {
        if (OnCacheItemExpired != null) {
            OnCacheItemExpired(pKey, pItem);
        }
    }

    protected virtual void RaiseCacheItemRemoved (TCacheKey pKey, CacheItem<TCacheValue, TTimeStamp> pItem) {
        if (OnCacheItemRemoved != null) {
            OnCacheItemRemoved(pKey, pItem);
        }
    }
}

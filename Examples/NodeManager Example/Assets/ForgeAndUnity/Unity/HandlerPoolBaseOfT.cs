using System;
using System.Collections.Generic;
using System.Linq;

namespace ForgeAndUnity.Unity {

    /// <summary>
    /// Class for generating identifiers for various needs like sessionIDs, EntityIDs, HandlerIDs etc.
    /// </summary>
    public abstract class HandlerPoolBaseOfT<T> where T : struct, IComparable<T> {
        //Fields
        protected T                         _startAtIdentifier;
        protected T                         _nextIdentifier;
        protected T                         _lowerBound;
        protected T                         _upperBound;
        protected HashSet<T>                _identifiers;
        protected HashSet<T>                _freeIds;
        protected bool                      _useFreeIds;

        public T                            NextIdentifier              { get { return _nextIdentifier; } set { _nextIdentifier = value; } }
        public T                            LowerBound                  { get { return _lowerBound; } set { _lowerBound = value; } }
        public T                            UpperBound                  { get { return _upperBound; } set { _upperBound = value; } }
        public HashSet<T>                   Identifiers                 { get { return _identifiers; } }
        public HashSet<T>                   FreeIds                     { get { return _freeIds; } }
        public bool                         UseFreeIds                  { get { return _useFreeIds; } set { _useFreeIds = value; } }


        //Functions
        public HandlerPoolBaseOfT (T pLowerBound, T pUpperBound, T pStartAtIdentifier = default(T), bool pUseFreeIds = true) {
            _lowerBound = pLowerBound;
            _upperBound = pUpperBound;
            _startAtIdentifier = pStartAtIdentifier;
            _nextIdentifier = _startAtIdentifier;
            _identifiers = new HashSet<T>();
            _freeIds = new HashSet<T>();
            _useFreeIds = pUseFreeIds;
        }

        #region Helpers
        public virtual T GetNext () {
            T newID = default(T);
            if (_freeIds.Count > 0) {
                newID = _freeIds.First();
                Add(newID);
            } else {
                //Never use while loops!
                int loopLimit = _identifiers.Count + 1;
                for (int i = 0; i < loopLimit; i++) {
                    if (!IsInBounds(_nextIdentifier)) {
                        _nextIdentifier = _lowerBound;
                    }

                    if (Add(_nextIdentifier)) {
                        newID = _nextIdentifier;
                        break;
                    }

                    _nextIdentifier = NextIdentifierIncrement(_nextIdentifier);
                }
            }

            return newID;
        }

        public virtual T PeekNext () {
            T newID = default(T);
            if (_freeIds.Count > 0) {
                newID = _freeIds.First();
            } else {
                //Never use while loops!
                T tmpIdentifier = _nextIdentifier;
                int loopLimit = _identifiers.Count + 1;
                for (int i = 0; i < loopLimit; i++) {
                    if (!IsInBounds(tmpIdentifier)) {
                        tmpIdentifier = _lowerBound;
                    }

                    if (!Contains(tmpIdentifier)) {
                        newID = tmpIdentifier;
                        break;
                    }

                    tmpIdentifier = NextIdentifierIncrement(tmpIdentifier);
                }
            }

            return newID;
        }

        public virtual void Free (T pIdentifier) {
            if (!_identifiers.Contains(pIdentifier)) {
                return;
            }

            _identifiers.Remove(pIdentifier);
            if (_freeIds.Contains(pIdentifier) || !_useFreeIds) {
                return;
            }

            _freeIds.Add(pIdentifier);
        }

        public virtual bool Add (T pIdentifier) {
            if (_identifiers.Contains(pIdentifier) || !IsInBounds(pIdentifier)) {
                return false;
            }

            if (_freeIds.Contains(pIdentifier)) {
                _freeIds.Remove(pIdentifier);
            }

            return _identifiers.Add(pIdentifier);
        }

        public virtual bool Contains (T pIdentifier) {
            return _identifiers.Contains(pIdentifier);
        }

        public virtual bool IsInBounds (T pIdentifier) {
            return _lowerBound.CompareTo(pIdentifier) <= 0 && _upperBound.CompareTo(pIdentifier) >= 0;
        }

        public virtual void Clear () {
            _nextIdentifier = _startAtIdentifier;
            _identifiers.Clear();
            _freeIds.Clear();
        }

        protected abstract T NextIdentifierIncrement (T pNextIdentifier); // Provide an implementation for incrementing the _nextIdentifier.

        #endregion
    }
}


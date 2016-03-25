namespace PetaPoco.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    internal class Cache<TKey, TValue>
    {
        private ReaderWriterLockSlim _lock;
        private Dictionary<TKey, TValue> _map;

        public Cache()
        {
            this._map = new Dictionary<TKey, TValue>();
            this._lock = new ReaderWriterLockSlim();
        }

        public void Flush()
        {
            this._lock.EnterWriteLock();
            try
            {
                this._map.Clear();
            }
            finally
            {
                this._lock.ExitWriteLock();
            }
        }

        public TValue Get(TKey key, Func<TValue> factory)
        {
            TValue local;
            TValue local2;
            this._lock.EnterReadLock();
            try
            {
                if (this._map.TryGetValue(key, out local))
                {
                    return local;
                }
            }
            finally
            {
                this._lock.ExitReadLock();
            }
            this._lock.EnterWriteLock();
            try
            {
                if (this._map.TryGetValue(key, out local))
                {
                    return local;
                }
                local = factory();
                this._map.Add(key, local);
                local2 = local;
            }
            finally
            {
                this._lock.ExitWriteLock();
            }
            return local2;
        }

        public int Count
        {
            get
            {
                return this._map.Count;
            }
        }
    }
}


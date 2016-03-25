namespace PetaPoco
{
    using PetaPoco.Internal;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    public static class Mappers
    {
        private static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private static Dictionary<object, IMapper> _mappers = new Dictionary<object, IMapper>();

        private static void FlushCaches()
        {
            MultiPocoFactory.FlushCaches();
            PocoData.FlushCaches();
        }

        public static IMapper GetMapper(Type t)
        {
            IMapper instance;
            _lock.EnterReadLock();
            try
            {
                IMapper mapper;
                if (_mappers.TryGetValue(t, out mapper))
                {
                    return mapper;
                }
                if (_mappers.TryGetValue(t.Assembly, out mapper))
                {
                    return mapper;
                }
                instance = Singleton<StandardMapper>.Instance;
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return instance;
        }

        public static void Register(Assembly assembly, IMapper mapper)
        {
            RegisterInternal(assembly, mapper);
        }

        public static void Register(Type type, IMapper mapper)
        {
            RegisterInternal(type, mapper);
        }

        private static void RegisterInternal(object typeOrAssembly, IMapper mapper)
        {
            _lock.EnterWriteLock();
            try
            {
                _mappers.Add(typeOrAssembly, mapper);
            }
            finally
            {
                _lock.ExitWriteLock();
                FlushCaches();
            }
        }

        public static void Revoke(IMapper mapper)
        {
            Func<KeyValuePair<object, IMapper>, bool> predicate = null;
            _lock.EnterWriteLock();
            try
            {
                if (predicate == null)
                {
                    predicate = kvp => kvp.Value == mapper;
                }
                foreach (KeyValuePair<object, IMapper> pair in _mappers.Where<KeyValuePair<object, IMapper>>(predicate).ToList<KeyValuePair<object, IMapper>>())
                {
                    _mappers.Remove(pair.Key);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
                FlushCaches();
            }
        }

        public static void Revoke(Assembly assembly)
        {
            RevokeInternal(assembly);
        }

        public static void Revoke(Type type)
        {
            RevokeInternal(type);
        }

        private static void RevokeInternal(object typeOrAssembly)
        {
            _lock.EnterWriteLock();
            try
            {
                _mappers.Remove(typeOrAssembly);
            }
            finally
            {
                _lock.ExitWriteLock();
                FlushCaches();
            }
        }
    }
}


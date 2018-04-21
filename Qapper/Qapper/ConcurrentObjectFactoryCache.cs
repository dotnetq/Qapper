using System;
using System.Collections.Concurrent;

namespace DotnetQ.Qapper
{
    internal class ConcurrentObjectFactoryCache<KeyT, ValueT>
    {
        public Func<KeyT, ValueT> FactoryMethod { get; protected set; }

        private readonly ConcurrentDictionary<KeyT, ValueT> cache = new ConcurrentDictionary<KeyT, ValueT>();

        public ConcurrentObjectFactoryCache(Func<KeyT, ValueT> factoryMethod)
        {
            FactoryMethod = factoryMethod;
        }

        public ConcurrentObjectFactoryCache()
        { }

        public ValueT this[KeyT key]
        {
            get
            {
                ValueT value;
                if (cache.TryGetValue(key, out value))
                {
                    return value;
                }

                value = FactoryMethod(key);
                cache.AddOrUpdate(key, value, (k, v) => v);
                return value;
            }
        }
    }

}


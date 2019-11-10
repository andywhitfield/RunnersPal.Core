using System;
using System.Collections.Concurrent;

namespace RunnersPal.Core.Data.Caching
{
    public class SimpleDataCache : IDataCache
    {
        private ConcurrentDictionary<string, object> cache = new ConcurrentDictionary<string, object>();

        public T Get<T>(string cacheKey, Func<T> valueFactory)
        {
            var obj = cache.GetOrAdd(cacheKey, _ => valueFactory());
            return (obj is T t) ? t : default(T);
        }
    }
}
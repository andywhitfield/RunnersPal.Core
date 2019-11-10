using System;

namespace RunnersPal.Core.Data.Caching
{
    public interface IDataCache
    {
        T Get<T>(string cacheKey, Func<T> valueFactory);
    }
}
using Aspector.Core.Attributes.Caching;
using Aspector.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aspector.Core.Caching
{
    public class CacheResultAsyncAspect : AsyncResultDecorator<CacheResultAsyncAttribute, object>
    {
        private readonly IMemoryCache _memoryCache;

        public CacheResultAsyncAspect(IMemoryCache memoryCache, ILoggerFactory loggerFactory, int layerIndex)
            : base(loggerFactory, layerIndex)
        {
            _memoryCache = memoryCache;
        }

        protected override async Task<object> Decorate(
            Func<object[]?, Task<object>> targetMethod,
            object[]? parameters,
            DecorationContext context,
            IEnumerable<CacheResultAsyncAttribute> aspectParameters)
        {
            var firstArg = aspectParameters.First();
            var cacheKey = firstArg.CacheKey ?? $"{context.DecoratedType.FullName}.{context.DecoratedMethod.Name}";
            if (_memoryCache.TryGetValue(cacheKey, out var cachedValue)
                && cachedValue != null)
            {
                return cachedValue;
            }

            cachedValue = await targetMethod(parameters);

            using var cacheEntry = _memoryCache.CreateEntry(cacheKey);
            cacheEntry.Value = cachedValue;

            if (firstArg.TimeToCacheMilliseconds.HasValue)
            {
                var timeSpan = TimeSpan.FromMilliseconds(firstArg.TimeToCacheMilliseconds.Value);
                cacheEntry.AbsoluteExpirationRelativeToNow = timeSpan;
                if (firstArg.SlidingExpiration)
                {
                    cacheEntry.SlidingExpiration = timeSpan;
                }
            }

            return cachedValue;
        }
    }
}

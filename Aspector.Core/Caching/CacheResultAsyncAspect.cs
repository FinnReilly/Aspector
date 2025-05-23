using Aspector.Core.Attributes.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Reflection;

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
            (ParameterInfo[] ParameterMetadata, MethodInfo DecoratedMethod, Type DecoratedType) decorationContext,
            IEnumerable<CacheResultAsyncAttribute> aspectParameters)
        {
            var firstArg = aspectParameters.First();
            var cacheKey = firstArg.CacheKey ?? $"{decorationContext.DecoratedType.FullName}.{decorationContext.DecoratedMethod.Name}";
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

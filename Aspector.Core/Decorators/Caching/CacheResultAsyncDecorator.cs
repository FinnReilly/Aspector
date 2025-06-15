using Aspector.Core.Attributes.Caching;
using Aspector.Core.Models;
using Aspector.Core.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Aspector.Core.Decorators.Caching
{
    public class CacheResultAsyncDecorator<TResult> : AsyncResultDecorator<CacheResultAsyncAttribute<TResult>, TResult>
    {
        private readonly IMemoryCache _memoryCache;

        public CacheResultAsyncDecorator(IMemoryCache memoryCache, IDecoratorServices services, int layerIndex)
            : base(services, layerIndex)
        {
            _memoryCache = memoryCache;
        }

        protected override async Task<TResult> Decorate(
            Func<object[]?, Task<TResult>> targetMethod,
            object[]? parameters,
            DecorationContext context,
            IEnumerable<CacheResultAsyncAttribute<TResult>> aspectParameters)
        {
            var firstArg = aspectParameters.First();
            var cacheKey = firstArg.CacheKey ?? GetDefaultCacheKey(context);

            if (firstArg.CacheKeyParameter != null)
            {
                cacheKey = context.GetParameterByName(firstArg.CacheKeyParameter, parameters ?? []);
                if (cacheKey == null)
                {
                    cacheKey = firstArg.NullCacheKeyBehaviour == NullCacheKeyBehaviour.UseFallback ?
                        GetDefaultCacheKey(context)
                        : throw new ArgumentNullException(
                            nameof(firstArg.CacheKeyParameter),
                            "Could not use as a cache key");
                }
            }

            if (_memoryCache.TryGetValue(cacheKey, out var cachedValue)
                && cachedValue != null
                && cachedValue is TResult cachedValueAsResult)
            {
                return cachedValueAsResult;
            }

            var methodResult = await targetMethod(parameters);
            cachedValue = methodResult;

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

            return methodResult;
        }

        private string GetDefaultCacheKey(DecorationContext context) => $"{context.DecoratedType.FullName}.{context.DecoratedMethod.Name}";
    }
}

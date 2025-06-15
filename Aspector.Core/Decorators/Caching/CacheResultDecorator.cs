using Aspector.Core.Attributes.Caching;
using Aspector.Core.Decorators;
using Aspector.Core.Models;
using Aspector.Core.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Aspector.Core.Decorators.Caching
{
    public class CacheResultDecorator<TResult> : ResultDecorator<CacheResultAttribute<TResult>, TResult>
    {
        private readonly IMemoryCache _memoryCache;

        public CacheResultDecorator(IMemoryCache memoryCache, IDecoratorServices services, int layerIndex)
            : base(services, layerIndex)
        {
            _memoryCache = memoryCache;
        }

        protected override TResult Decorate(
            Func<object[]?, TResult> targetMethod,
            object[]? parameters,
            DecorationContext context,
            IEnumerable<CacheResultAttribute<TResult>> aspectParameters)
        {
            var aspectParameter = aspectParameters.First();
            var cacheKey = aspectParameter.CacheKey ?? GetDefaultCacheKey(context);

            if (aspectParameter.CacheKeyParameter != null)
            {
                cacheKey = context.GetParameterByName(aspectParameter.CacheKeyParameter, parameters ?? []);
                if (cacheKey == null)
                {
                    cacheKey = aspectParameter.NullCacheKeyBehaviour == NullCacheKeyBehaviour.UseFallback ?
                        GetDefaultCacheKey(context)
                        : throw new ArgumentNullException(
                            nameof(aspectParameter.CacheKeyParameter),
                            "Could not use as a cache key");
                }
            }

            if (_memoryCache.TryGetValue(cacheKey, out var cachedValue)
                && cachedValue != null
                && cachedValue is TResult cachedValueAsResult)
            {
                return cachedValueAsResult;
            }

            var result = targetMethod(parameters);
            using var entry = _memoryCache.CreateEntry(cacheKey);
            entry.Value = result;

            if (aspectParameter.TimeToCacheMilliseconds.HasValue)
            {
                var timeSpan = TimeSpan.FromMilliseconds(aspectParameter.TimeToCacheMilliseconds.Value);
                entry.AbsoluteExpirationRelativeToNow = timeSpan;
                if (aspectParameter.SlidingExpiration)
                {
                    entry.SlidingExpiration = timeSpan;
                }
            }

            return result;
        }

        private string GetDefaultCacheKey(DecorationContext context) => $"{context.DecoratedType?.FullName}.{context.DecoratedMethod.Name}";
    }
}

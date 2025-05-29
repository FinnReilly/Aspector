using Aspector.Core.Attributes.Caching;
using Aspector.Core.Models;
using Aspector.Core.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Aspector.Core.Caching
{
    public class CacheResultAspect : ResultDecorator<CacheResultAttribute, object>
    {
        private readonly IMemoryCache _memoryCache;

        public CacheResultAspect(IMemoryCache memoryCache, IDecoratorServices services, int layerIndex)
            : base(services, layerIndex)
        {
            _memoryCache = memoryCache;
        }

        protected override object Decorate(Func<object[]?, object> targetMethod, object[]? parameters, DecorationContext context, IEnumerable<CacheResultAttribute> aspectParameters)
        {
            var aspectParameter = aspectParameters.First();
            var cacheKey = aspectParameter.CacheKey ?? $"{context.DecoratedType?.FullName}.{context.DecoratedMethod.Name}";

            if (_memoryCache.TryGetValue(cacheKey, out var cachedValue)
                && cachedValue != null)
            {
                return cachedValue;
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
    }
}

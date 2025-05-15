using Aspector.Core.Attributes.Caching;
using Castle.Components.DictionaryAdapter.Xml;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aspector.Core.Caching
{
    public class CacheResultAsyncAspect : AsyncResultDecorator<CacheResultAsyncAttribute, object>
    {
        private readonly IMemoryCache _memoryCache;

        public CacheResultAsyncAspect(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        protected override async Task<object> Decorate(Func<object[]?, Task<object>> targetMethod, object[]? parameters, (IEnumerable<ParameterInfo> ParameterMetadata, MethodInfo DecoratedMethod, Type DecoratedType) decorationContext, IEnumerable<CacheResultAsyncAttribute> aspectParameters)
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

using Aspector.Core.Attributes;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;

namespace Aspector.Core.Caching
{
    public class CacheResultAspect : BaseAspectImplementation<CacheResultAttribute>
    {
        private readonly IMemoryCache _memoryCache;

        public CacheResultAspect(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        protected override void Decorate(IInvocation invocation, IEnumerable<CacheResultAttribute> aspectParameters)
        {
            var aspectParameter = aspectParameters.First();
            var cacheKey = aspectParameter.CacheKey ?? $"{invocation.TargetType?.FullName}.{invocation.Method.Name}";

            if (_memoryCache.TryGetValue(cacheKey, out var cachedValue)
                && cachedValue != null
                && cachedValue.GetType() == invocation.Method.ReturnType)
            {
                invocation.ReturnValue = cachedValue;
                return;
            }

            invocation.Proceed();

            var entry = _memoryCache.CreateEntry(cacheKey);
            entry.Value = cachedValue;
            var timeSpan = TimeSpan.FromMilliseconds(aspectParameter.TimeToCacheMilliseconds);
            if (aspectParameter.SlidingExpiration)
            {
                entry.SlidingExpiration = timeSpan;
            }
            else
            {
                entry.AbsoluteExpirationRelativeToNow = timeSpan;
            }
        }
    }
}

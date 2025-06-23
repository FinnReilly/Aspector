namespace Aspector.Core.Attributes.Caching
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CacheResultAttribute<TResult> : AspectAttribute
    {
        private object? _cacheKey;

        public int? TimeToCacheMilliseconds { get; }
        public bool SlidingExpiration { get; }
        /// <summary>
        /// Attention - setting this will override any cache key parameter
        /// </summary>
        public object? CacheKey
        {
            get => _cacheKey;
            set
            {
                CacheKeyParameter = null;
                _cacheKey = value!;
            }
        }
        public NullCacheKeyBehaviour NullCacheKeyBehaviour { get; set; } = NullCacheKeyBehaviour.Throw;
        public string? CacheKeyParameter { get; private set; } = null;
        public Type Type { get; } = typeof(TResult);

        public CacheResultAttribute(double timeToCacheSeconds)
        {
            TimeToCacheMilliseconds = (int)Math.Floor(timeToCacheSeconds * 1000);
        }

        public CacheResultAttribute(double timeToCacheSeconds, string keyParameter)
            : this(timeToCacheSeconds)
        {
            CacheKeyParameter = keyParameter;
        }

        public CacheResultAttribute(double timeToCacheSeconds, bool slidingExpiration)
            : this(timeToCacheSeconds)
        {
            SlidingExpiration = slidingExpiration;
        }

        public CacheResultAttribute(double timeToCacheSeconds, bool slidingExpiration, string keyParameter)
            : this(timeToCacheSeconds, slidingExpiration)
        {
            CacheKeyParameter = keyParameter;
        }
    }
}

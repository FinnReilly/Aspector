namespace Aspector.Core.Attributes.Caching
{
    /// <summary>
    /// Caches the result of a method according to configured parameters
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the decorated method</typeparam>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CacheResultAttribute<TResult> : AspectAttribute
    {
        private object? _cacheKey;

        public int? TimeToCacheMilliseconds { get; }
        public bool SlidingExpiration { get; }

        /// <summary>
        /// Gets and sets a single hardcoded cache key for all results
        /// <br/><br/>
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

        /// <summary>
        /// Gets or sets an enum describing how to behave if a parameter to be used for a cache key is null.
        /// <br/><br/>
        /// The default is <see cref="NullCacheKeyBehaviour.Throw"/>
        /// </summary>
        public NullCacheKeyBehaviour NullCacheKeyBehaviour { get; set; } = NullCacheKeyBehaviour.Throw;
        public string? CacheKeyParameter { get; private set; } = null;
        public Type Type { get; } = typeof(TResult);

        /// <param name="timeToCacheSeconds">The amount of time to cache the result in seconds</param>
        public CacheResultAttribute(double timeToCacheSeconds)
        {
            TimeToCacheMilliseconds = (int)Math.Floor(timeToCacheSeconds * 1000);
        }

        /// <param name="timeToCacheSeconds">The amount of time to cache the result in seconds</param>
        /// <param name="cacheKeyParameter">The name of a parameter whose value will be used as a cache key</param>
        public CacheResultAttribute(double timeToCacheSeconds, string cacheKeyParameter)
            : this(timeToCacheSeconds)
        {
            CacheKeyParameter = cacheKeyParameter;
        }

        /// <param name="timeToCacheSeconds">The amount of time to cache the result in seconds</param>
        /// <param name="slidingExpiration">Whether to use sliding expiration</param>
        public CacheResultAttribute(double timeToCacheSeconds, bool slidingExpiration)
            : this(timeToCacheSeconds)
        {
            SlidingExpiration = slidingExpiration;
        }

        /// <param name="timeToCacheSeconds">The amount of time to cache the result in seconds</param>
        /// <param name="slidingExpiration">Whether to use sliding expiration</param>
        /// <param name="cacheKeyParameter">The name of a parameter whose value will be used as a cache key</param>
        public CacheResultAttribute(double timeToCacheSeconds, bool slidingExpiration, string cacheKeyParameter)
            : this(timeToCacheSeconds, slidingExpiration)
        {
            CacheKeyParameter = cacheKeyParameter;
        }
    }
}

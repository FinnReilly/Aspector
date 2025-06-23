namespace Aspector.Core.Attributes.Caching
{
    /// <summary>
    /// Caches the result of a async method according to configured parameters
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by an awaitable <see cref="Task{TResult}"/> returned by the decorated method</typeparam>
    
    public class CacheResultAsyncAttribute<TResult> : CacheResultAttribute<TResult>
    {
        /// <inheritdoc/>
        public CacheResultAsyncAttribute(double timeToCacheSeconds) 
            : base(timeToCacheSeconds)
        {
        }

        /// <inheritdoc/>
        public CacheResultAsyncAttribute(double timeToCacheSeconds, string cacheKeyParameter)
            : base(timeToCacheSeconds, cacheKeyParameter)
        {
        }

        /// <inheritdoc/>
        public CacheResultAsyncAttribute(double timeToCacheSeconds, bool slidingExpiration) 
            : base(timeToCacheSeconds, slidingExpiration)
        {
        }

        /// <inheritdoc/>
        public CacheResultAsyncAttribute(double timeToCacheSeconds, bool slidingExpiration, string cacheKeyParameter) 
            : base(timeToCacheSeconds, slidingExpiration, cacheKeyParameter)
        { 
        }
    }
}

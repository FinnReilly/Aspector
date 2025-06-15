namespace Aspector.Core.Attributes.Caching
{
    ///</inheritdoc>
    public class CacheResultAsyncAttribute<TResult> : CacheResultAttribute<TResult>
    {
        public CacheResultAsyncAttribute(double timeToCacheSeconds) 
            : base(timeToCacheSeconds)
        {
        }

        public CacheResultAsyncAttribute(double timeToCacheSeconds, string cacheKeyParameter)
            : base(timeToCacheSeconds, cacheKeyParameter)
        {
        }

        public CacheResultAsyncAttribute(double timeToCacheSeconds, bool slidingExpiration) 
            : base(timeToCacheSeconds, slidingExpiration)
        {
        }

        public CacheResultAsyncAttribute(double timeToCacheSeconds, bool slidingExpiration, string cacheKeyParameter) 
            : base(timeToCacheSeconds, slidingExpiration, cacheKeyParameter)
        { 
        }
    }
}

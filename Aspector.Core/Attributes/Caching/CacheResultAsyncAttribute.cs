namespace Aspector.Core.Attributes.Caching
{
    public class CacheResultAsyncAttribute<TResult> : CacheResultAttribute<TResult>
    {
        public CacheResultAsyncAttribute(double timeToCacheSeconds) 
            : base(timeToCacheSeconds)
        {
        }

        public CacheResultAsyncAttribute(double timeToCacheSeconds, bool slidingExpiration) 
            : base(timeToCacheSeconds, slidingExpiration)
        {
        }
    }
}

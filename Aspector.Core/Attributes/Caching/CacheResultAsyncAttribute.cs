namespace Aspector.Core.Attributes.Caching
{
    public class CacheResultAsyncAttribute : CacheResultAttribute
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

namespace Aspector.Core.Attributes
{
    public class CacheResultAttribute : AspectAttribute
    {
        public int TimeToCacheMilliseconds { get; }
        public bool SlidingExpiration { get; }
        public string? CacheKey { get; set; } = null;

        public CacheResultAttribute(int timeToCacheMilliseconds, bool slidingExpiration)
        {
            TimeToCacheMilliseconds = timeToCacheMilliseconds;
            SlidingExpiration = slidingExpiration;
        }
    }
}

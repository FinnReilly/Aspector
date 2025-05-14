namespace Aspector.Core.Attributes
{
    public class CacheResultAttribute : AspectAttribute
    {
        public int? TimeToCacheMilliseconds { get; }
        public bool SlidingExpiration { get; }
        public string? CacheKey { get; set; } = null;

        public CacheResultAttribute() { }

        public CacheResultAttribute(double timeToCacheSeconds)
        {
            TimeToCacheMilliseconds = (int)Math.Floor(timeToCacheSeconds * 1000);
        }

        public CacheResultAttribute(double timeToCacheSeconds, bool slidingExpiration)
        {
            TimeToCacheMilliseconds = (int)Math.Floor(timeToCacheSeconds * 1000);
            SlidingExpiration = slidingExpiration;
        }
    }
}

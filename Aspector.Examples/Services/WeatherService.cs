using Aspector.Core.Attributes.Caching;
using Aspector.Core.Attributes.Logging;
using Aspector.Models;

namespace Aspector.Services
{
    public class WeatherService : IWeatherService
    {
        private string[] summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherService> _logger;

        public WeatherService(ILogger<WeatherService> logger)
        {
            _logger = logger;
        }

        [AddLogProperty("IsFromCache", ConstantValue = true)]
        [AddLogProperty("QueryType", ConstantValue = "Weather")]
        [Log("Will check cache for result first")]
        [CacheResult<IEnumerable<WeatherForecast>>(timeToCacheSeconds: 10, slidingExpiration: false, CacheKey = 5)]
        [Log("Cache must have expired, refreshing", LogLevel.Warning)]
        public IEnumerable<WeatherForecast> GetWeather()
        {
            using var logScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["IsFromCache"] = false
            });

            return GetWeatherForNextNDays(5);
        }

        public IEnumerable<WeatherForecast> GetWeatherForNextNDays(int n)
        {
            _logger.LogInformation("Getting fresh data");
            var forecast = Enumerable.Range(1, n).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();

            return forecast;
        }

        [CacheResultAsync<IEnumerable<WeatherForecast>>(timeToCacheSeconds: 5.2, cacheKeyParameter: "n")]
        [Log("No cached result found for {weatherForecastCount}, fetching fresh data", LogLevel.Warning, "n")]
        public Task<IEnumerable<WeatherForecast>> GetWeatherAsync(int n)
        {
            return Task.FromResult(GetWeatherForNextNDays(n));
        }
    }
}

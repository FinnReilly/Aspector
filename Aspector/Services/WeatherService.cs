using Aspector.Core.Attributes;
using Aspector.Models;

namespace Aspector.Services
{
    public class WeatherService : IWeatherService
    {
        private string[] summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [CacheResult(timeToCacheMilliseconds: 10000, slidingExpiration: false)]
        public IEnumerable<WeatherForecast> GetWeather()
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();

            return forecast;
        }
    }
}

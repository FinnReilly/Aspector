using Aspector.Models;

namespace Aspector.Services
{
    public interface IWeatherService
    {
        IEnumerable<WeatherForecast> GetWeather();

        IEnumerable<WeatherForecast> GetWeatherForNextNDays(int n);

        Task<IEnumerable<WeatherForecast>> GetWeatherAsync(int n);

        Task<IEnumerable<WeatherForecast>> GetWeatherForNextNDaysFromAsync(int n, DateTime minimumDateUtc);
    }
}

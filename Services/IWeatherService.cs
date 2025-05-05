using Aspector.Models;

namespace Aspector.Services
{
    public interface IWeatherService
    {
        IEnumerable<WeatherForecast> GetWeather();
    }
}

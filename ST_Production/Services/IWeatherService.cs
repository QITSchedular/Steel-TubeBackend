
namespace ST_Production.Services
{
    public interface IWeatherService
    {
        Task<WeatherForecast> GetWeatherForecast(string cityName, bool isAirQualityNeeded);
    }
}

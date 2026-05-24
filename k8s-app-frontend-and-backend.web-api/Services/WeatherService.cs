using k8s_app_frontend_and_backend.web_api.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace k8s_app_frontend_and_backend.web_api.Services
{
  public class WeatherService
  {
    private readonly IDatabase _db;

    public WeatherService(IConnectionMultiplexer redis)
    {
      _db = redis.GetDatabase();
    }

    public async Task SaveForecastAsync(WeatherForecast forecast)
    {
      string json = JsonSerializer.Serialize(forecast);

      string key = $"weather:{forecast.Id}";

      await _db.StringSetAsync(key, json, TimeSpan.FromHours(24));
    }
  }
}

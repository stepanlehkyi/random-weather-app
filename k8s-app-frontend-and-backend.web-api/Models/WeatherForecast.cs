namespace k8s_app_frontend_and_backend.web_api.Models
{
  public class WeatherForecast
  {
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
  }
}

namespace k8s_app_frontend_and_backend.web_api.Models.DTOs
{
  public class WeatherForecastCreateDto
  {
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
  }
}

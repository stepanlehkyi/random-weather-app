namespace k8s_app_frontend_and_backend.web_api.Configuration
{
  /// <summary>
  /// Configuration for Weather API settings - typically loaded from appsettings.json or environment variables
  /// Can be injected via Kubernetes ConfigMaps and Secrets
  /// </summary>
  public class WeatherApiConfig
  {
    public const string SectionName = "WeatherApi";

    /// <summary>
    /// Maximum number of days to forecast (configurable via ConfigMap)
    /// </summary>
    public int MaxForecastDays { get; set; } = 5;

    /// <summary>
    /// Minimum temperature in Celsius (configurable via ConfigMap)
    /// </summary>
    public int MinTemperatureC { get; set; } = -20;

    /// <summary>
    /// Maximum temperature in Celsius (configurable via ConfigMap)
    /// </summary>
    public int MaxTemperatureC { get; set; } = 55;

    /// <summary>
    /// Enable caching of forecast results (configurable via ConfigMap)
    /// </summary>
    public bool EnableCaching { get; set; } = false;

    /// <summary>
    /// Cache duration in seconds (configurable via ConfigMap)
    /// </summary>
    public int CacheDurationSeconds { get; set; } = 300;

    /// <summary>
    /// API version (configurable via ConfigMap)
    /// </summary>
    public string ApiVersion { get; set; } = "1.0";
  }
}

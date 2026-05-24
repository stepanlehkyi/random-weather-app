using k8s_app_frontend_and_backend.web_api.Configuration;
using k8s_app_frontend_and_backend.web_api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddHealthChecks()
    // Liveness: Just checks if the app process is running. No dependencies.
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

builder.Services.AddHealthChecks()
    // This automatically pings Redis to see if it's responding
    .AddRedis(redisConnectionString, name: "redis", tags: new[] { "ready" });

// Add services to the container.
builder.Services.AddControllers();

// Configure WeatherApi settings from appsettings.json or environment variables (K8s ConfigMaps)
builder.Services.Configure<WeatherApiConfig>(
    builder.Configuration.GetSection(WeatherApiConfig.SectionName));
builder.Services.AddSingleton<WeatherService>();
// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        policy =>
        {
 policy.AllowAnyOrigin()
      .AllowAnyMethod()
    .AllowAnyHeader();
        });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Map Liveness endpoint
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
  Predicate = check => check.Tags.Contains("live")
});

// Map Readiness endpoint
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
  Predicate = check => check.Tags.Contains("ready")
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS - must be before UseHttpsRedirection
app.UseCors("AllowAngularDev");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

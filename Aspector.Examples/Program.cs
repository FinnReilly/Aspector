using Aspector.Core.Extensions;
using Aspector.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IWeatherService, WeatherService>();

builder.Services.AddAspects();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
app.MapGet(
    "/weatherforecast",
    (IWeatherService service, ILogger<Program> logger) => 
    {
        using var logScope = logger.BeginScope(new Dictionary<string, object>
        {
            { "QueryType", "WeatherOfCourse" }
        });
        return service.GetWeather();
    });

app.MapGet(
    "/weatherforecast/{days}",
    async (IWeatherService service, [FromRoute] int days) => await service.GetWeatherAsync(days));

app.Run();

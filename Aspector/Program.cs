using Aspector.Core.Extensions;
using Aspector.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IWeatherService, WeatherService>();
builder.Services.AddMemoryCache();

builder.Services.AddAspects();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
app.MapGet("/weatherforecast", (IWeatherService service) => service.GetWeather());

app.Run();

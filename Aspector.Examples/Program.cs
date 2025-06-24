using Aspector.Core.Extensions;
using Aspector.Examples.Data;
using Aspector.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IWeatherService, WeatherService>();
builder.Services.AddDbContext<ExampleContext>(
    opt => 
        opt.UseSqlite(new SqliteConnection("Data Source=Application.db;Cache=Shared")));
builder.Services.AddHostedService<DataSeeder>();

builder.Services.AddAspects();

var app = builder.Build();

// migrations
using (var serviceScope = app.Services.CreateScope())
{
    await serviceScope.ServiceProvider.GetRequiredService<ExampleContext>().Database.MigrateAsync();
}

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

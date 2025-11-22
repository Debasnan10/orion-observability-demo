using Serilog;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog (reads from appsettings.json) ---
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration);
});

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Orion API", Version = "v1" });
});

// --- Health Checks (required before MapHealthChecks) ---
builder.Services.AddHealthChecks();

// --- OpenTelemetry Metrics + Prometheus exporter ---
var serviceName = "Orion.Api";
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("Orion.Orders")       // for your custom business metrics later
            .AddPrometheusExporter();       // exposes /metrics for Prometheus
    });

var app = builder.Build();

// --- Middleware / Endpoints ---
app.UseSerilogRequestLogging();

// Prometheus scraping endpoint (default path is /metrics)
app.MapPrometheusScrapingEndpoint("/metrics");

// Health endpoint
app.MapHealthChecks("/health");

// Swagger only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Skip HTTPS redirection for now to avoid local warning
// app.UseHttpsRedirection();

// Demo endpoint
var summaries = new[]
{
    "Freezing","Bracing","Chilly","Cool","Mild","Warm","Balmy","Hot","Sweltering","Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )
    ).ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

if (args.Contains("--health-check", StringComparer.OrdinalIgnoreCase))
{
    Environment.ExitCode = await ProbeHealthAsync();
    return;
}

var startedAt = DateTimeOffset.UtcNow;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    const string header = "X-Correlation-ID";
    var correlationId = context.Request.Headers[header].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 128)
    {
        correlationId = Guid.NewGuid().ToString("N");
    }

    context.TraceIdentifier = correlationId;
    context.Response.Headers[header] = correlationId;
    await next();
});

app.MapGet("/", () => Results.Ok(new
{
    service = "Dockyard",
    description = "Container diagnostics and request inspection API",
    documentation = "/api/v1"
}));

app.MapGet("/api/v1", () => Results.Ok(new
{
    endpoints = new[]
    {
        "GET /api/v1/runtime",
        "GET /api/v1/inspect",
        "GET /health/live",
        "GET /health/ready"
    }
}));

app.MapGet("/api/v1/runtime", (IHostEnvironment environment) => Results.Ok(new
{
    service = "Dockyard",
    version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3),
    environment = environment.EnvironmentName,
    framework = RuntimeInformation.FrameworkDescription,
    operatingSystem = RuntimeInformation.OSDescription,
    architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    processorCount = Environment.ProcessorCount,
    startedAt,
    uptimeSeconds = Math.Round((DateTimeOffset.UtcNow - startedAt).TotalSeconds, 3)
}));

app.MapGet("/api/v1/inspect", (HttpContext context) =>
{
    var request = context.Request;

    return Results.Ok(new
    {
        correlationId = context.TraceIdentifier,
        request = new
        {
            request.Method,
            request.Scheme,
            request.Protocol,
            host = request.Host.Value,
            pathBase = request.PathBase.Value,
            path = request.Path.Value,
            queryString = request.QueryString.Value
        },
        client = new
        {
            remoteIp = context.Connection.RemoteIpAddress?.ToString(),
            userAgent = request.Headers.UserAgent.FirstOrDefault()
        },
        proxy = new
        {
            forwardedFor = request.Headers["X-Forwarded-For"].FirstOrDefault(),
            forwardedHost = request.Headers["X-Forwarded-Host"].FirstOrDefault(),
            forwardedProto = request.Headers["X-Forwarded-Proto"].FirstOrDefault()
        },
        timestamp = DateTimeOffset.UtcNow
    });
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready");

app.Run();

static async Task<int> ProbeHealthAsync()
{
    var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080";
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

    try
    {
        using var response = await client.GetAsync($"http://127.0.0.1:{port}/health/live");
        return response.IsSuccessStatusCode ? 0 : 1;
    }
    catch (HttpRequestException)
    {
        return 1;
    }
    catch (TaskCanceledException)
    {
        return 1;
    }
}

public partial class Program;

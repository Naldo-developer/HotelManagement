using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using HotelManagement.Api.Configuration;
using HotelManagement.Application.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotelManagement.Api.Tests;

public class ErrorHandlingTests
{
    [Theory]
    [InlineData("missing", 404, "El recurso solicitado no existe.")]
    [InlineData("conflict", 409, "El recurso ya existe.")]
    [InlineData("unexpected", 500, "Ocurrió un error inesperado. Inténtalo nuevamente.")]
    [InlineData("business-validation", 400, "La página debe ser mayor o igual a 1.")]
    public async Task Exceptions_return_problem_details(string error, int status, string detail)
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync($"/test-errors/{error}");
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(status, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(detail, json.RootElement.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("sensitive-internal-message", body);
        Assert.DoesNotContain("Password=", body);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unexpected_error_logs_safe_details_with_response_trace_id()
    {
        var logs = new CapturedLogs();
        await using var app = await StartApp(logs);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/test-errors/unexpected");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var traceId = json.RootElement.GetProperty("traceId").GetString()!;
        var errors = logs.Entries.Where(entry => entry.Level == LogLevel.Error).ToList();

        var error = Assert.Single(errors);
        Assert.Contains(traceId, error.Text);
        Assert.Contains(nameof(InvalidOperationException), error.Text);
        Assert.Contains(nameof(ErrorProbeController.Throw), error.Text);
        Assert.All(logs.Entries, entry =>
        {
            Assert.DoesNotContain("sensitive-internal-message", entry.Text);
            Assert.DoesNotContain("Password=", entry.Text);
        });
    }

    [Fact]
    public async Task Unsupported_accept_header_still_returns_safe_problem_json()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        using var response = await client.GetAsync("/test-errors/unexpected");
        Assert.Equal(500, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("sensitive-internal-message", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Api_controller_keeps_automatic_validation_400()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();
        using var response = await client.PostAsync("/test-errors/validation",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, (int)response.StatusCode);
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("Name", out _));
    }

    private static async Task<WebApplication> StartApp(CapturedLogs? logs = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        if (logs is not null)
        {
            builder.Logging.AddProvider(logs);
        }
        builder.Services.AddApiErrorHandling();
        builder.Services.AddControllers().AddApplicationPart(typeof(ErrorProbeController).Assembly);
        var app = builder.Build();
        app.UseExceptionHandler(new ExceptionHandlerOptions { SuppressDiagnosticsCallback = _ => true });
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private sealed class CapturedLogs : ILoggerProvider
    {
        public ConcurrentQueue<(LogLevel Level, string Text)> Entries { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CapturedLogger(this);
        public void Dispose() { }

        private sealed class CapturedLogger(CapturedLogs owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
                => owner.Entries.Enqueue((logLevel, formatter(state, exception) + exception));
        }
    }
}

// Solo se descubre en el servidor de pruebas; este ensamblado no se referencia desde Api.
[ApiController]
[Route("test-errors")]
public sealed class ErrorProbeController : ControllerBase
{
    [HttpGet("{error}")]
    public IActionResult Throw(string error)
    {
        throw error switch
        {
            "missing" => new ResourceNotFoundException("El recurso solicitado no existe."),
            "conflict" => new BusinessConflictException("El recurso ya existe."),
            "business-validation" => new BusinessValidationException("La página debe ser mayor o igual a 1."),
            _ => new InvalidOperationException("sensitive-internal-message;Password=test-secret")
        };
    }

    [HttpPost("validation")]
    public IActionResult ValidateInput(ValidationProbe input)
    {
        return Ok();
    }
}

public sealed class ValidationProbe
{
    [Required]
    public string? Name { get; set; }
}

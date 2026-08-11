using System.Text.Json;
using Jetar.Core.Common;

namespace Jetar.API.Middlewares;

/// <summary>
/// Barcha istisnolarni bir xil JSON shakliga keltiradi:
/// <c>{ "code": "...", "message": "...", "traceId": "..." }</c>
/// </summary>
public class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _log;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log, IHostEnvironment env)
    {
        _next = next;
        _log = log;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            // Kutilgan biznes xatosi — stack trace kerak emas.
            _log.LogInformation("Biznes xatosi {Code}: {Message}", ex.Code, ex.Message);
            await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Klient ulanishni uzdi — javob yozishga urinmaymiz.
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Kutilmagan xato: {Path}", context.Request.Path);

            await WriteAsync(context, 500, "internal_error",
                _env.IsDevelopment() ? ex.Message : "Serverda kutilmagan xato yuz berdi.");
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            code,
            message,
            traceId = context.TraceIdentifier
        }, Json));
    }
}

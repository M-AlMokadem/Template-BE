using API.Models;
using API.Services;

namespace API.Middlewares;

public class ErrorStatusLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IErrorLogService _errorLogService;
    private readonly IHostEnvironment _environment;

    public ErrorStatusLoggingMiddleware(
        RequestDelegate next,
        IErrorLogService errorLogService,
        IHostEnvironment environment)
    {
        _next = next;
        _errorLogService = errorLogService;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Items.ContainsKey("ExceptionHandled"))
        {
            return;
        }

        var statusCode = context.Response.StatusCode;
        if (statusCode < StatusCodes.Status400BadRequest)
        {
            return;
        }

        var correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = context.TraceIdentifier;
            context.Response.Headers["X-Correlation-ID"] = correlationId;
        }

        var entry = new ErrorLogEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Label = "error",
            StatusCode = statusCode,
            ErrorType = $"HTTP_{statusCode}",
            Message = $"Request finished with status code {statusCode}.",
            CorrelationId = correlationId,
            Method = context.Request.Method,
            Path = context.Request.Path.Value ?? string.Empty,
            QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
            TraceIdentifier = context.TraceIdentifier,
            Data = new
            {
                Environment = _environment.EnvironmentName
            }
        };

        await _errorLogService.LogAsync(entry, context.RequestAborted);
    }
}

public static class ErrorStatusLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorStatusLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ErrorStatusLoggingMiddleware>();
    }
}

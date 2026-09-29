using System.Net;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using API.Exceptions;
using API.Models;
using API.Services;
using Util.Core;

namespace API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IErrorLogService _errorLogService;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment,
        IErrorLogService errorLogService)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _errorLogService = errorLogService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            var correlationId = GetCorrelationId(context);
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}. CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            await HandleExceptionAsync(context, exception, correlationId, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        string correlationId,
        double durationMilliseconds)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("The response has already started; the exception middleware will not modify the response.");
            throw exception;
        }

        var statusCode = MapStatusCode(exception);
        context.Items["ExceptionHandled"] = true;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var logEntry = new ErrorLogEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Label = "exception",
            StatusCode = statusCode,
            ErrorType = exception.GetType().Name,
            Message = exception.Message,
            CorrelationId = correlationId,
            Method = context.Request.Method,
            Path = context.Request.Path.Value ?? string.Empty,
            QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
            TraceIdentifier = context.TraceIdentifier,
            DurationMilliseconds = durationMilliseconds,
            UserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            StackTrace = exception.StackTrace,
            Data = exception.Data
        };

        await _errorLogService.LogAsync(logEntry, context.RequestAborted);

        var message = _environment.IsDevelopment()
            ? exception.Message
            : "An unexpected error occurred.";

        object errorData;
        if (_environment.IsDevelopment())
        {
            errorData = new
            {
                CorrelationId = correlationId,
                StackTrace = exception.StackTrace
            };
        }
        else
        {
            errorData = new
            {
                CorrelationId = correlationId
            };
        }

        var payload = new DefaultResponse(
            success: false,
            statusCode: statusCode,
            data: errorData,
            message: message
        );

        var json = JsonSerializer.Serialize(payload);
        await context.Response.WriteAsync(json);
    }

    private static int MapStatusCode(Exception exception)
    {
        return exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            System.ComponentModel.DataAnnotations.ValidationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetCorrelationId(HttpContext context)
    {
        const string CorrelationHeaderName = "X-Correlation-ID";

        var correlationId = context.Request.Headers[CorrelationHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = context.TraceIdentifier;
        }

        return correlationId;
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
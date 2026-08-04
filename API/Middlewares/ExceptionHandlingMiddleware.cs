using System.Net;
using System.Text.Json;
using Util.Core;

namespace API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("The response has already started; the exception middleware will not modify the response.");
            throw exception;
        }

        var statusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var message = _environment.IsDevelopment()
            ? exception.Message
            : "An unexpected error occurred.";

        var payload = new DefaultResponse(
            success: false,
            statusCode: statusCode,
            data: _environment.IsDevelopment() ? new { exception.StackTrace } : null!,
            message: message
        );

        var json = JsonSerializer.Serialize(payload);
        await context.Response.WriteAsync(json);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
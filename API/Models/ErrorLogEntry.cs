namespace API.Models;

public class ErrorLogEntry
{
    public DateTimeOffset TimestampUtc { get; set; }
    public string Label { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public string? TraceIdentifier { get; set; }
    public string? StackTrace { get; set; }
    public object? Data { get; set; }
}

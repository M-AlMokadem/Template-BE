using System.Text.Json;
using API.Models;

namespace API.Services;

public interface IErrorLogService
{
    Task LogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default);
}

public class ErrorLogService : IErrorLogService
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);
    private readonly string _logsDirectory;
    private readonly JsonSerializerOptions _serializerOptions;

    public ErrorLogService(IHostEnvironment environment)
    {
        _logsDirectory = Path.Combine(environment.ContentRootPath, "Logs");
        Directory.CreateDirectory(_logsDirectory);

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task LogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
    {
        var fileName = $"errors-{DateTime.UtcNow:yyyy-MM-dd}.jsonl";
        var filePath = Path.Combine(_logsDirectory, fileName);
        var json = JsonSerializer.Serialize(entry, _serializerOptions);

        await FileLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(filePath, json + Environment.NewLine, cancellationToken);
        }
        finally
        {
            FileLock.Release();
        }
    }
}

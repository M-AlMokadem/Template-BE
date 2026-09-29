using Domain.Context;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API.Services;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationContext _applicationContext;

    public DatabaseHealthCheck(ApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _applicationContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL health check failed.", exception);
        }
    }
}

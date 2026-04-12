using CleanSync.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CleanSync.Api.HealthChecks;

public class SapConnectionHealthCheck : IHealthCheck
{
    private readonly ISapBusinessPartnerService _sapService;
    private readonly ILogger<SapConnectionHealthCheck> _logger;

    public SapConnectionHealthCheck(ISapBusinessPartnerService sapService, ILogger<SapConnectionHealthCheck> logger)
    {
        _sapService = sapService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking SAP connection health");
            var isConnected = await _sapService.TestConnectionAsync();
            
            if (isConnected)
            {
                _logger.LogInformation("SAP connection health check passed");
                return HealthCheckResult.Healthy("SAP connection is healthy");
            }
            
            _logger.LogWarning("SAP connection health check failed - connection returned false");
            return HealthCheckResult.Unhealthy("SAP connection test failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAP connection health check failed with exception");
            return HealthCheckResult.Unhealthy("SAP connection error", ex);
        }
    }
}

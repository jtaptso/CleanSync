using CleanSync.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CleanSync.Api.HealthChecks;

public class EcommerceConnectionHealthCheck : IHealthCheck
{
    private readonly IEcommerceBusinessPartnerService _ecommerceService;
    private readonly ILogger<EcommerceConnectionHealthCheck> _logger;

    public EcommerceConnectionHealthCheck(IEcommerceBusinessPartnerService ecommerceService, ILogger<EcommerceConnectionHealthCheck> logger)
    {
        _ecommerceService = ecommerceService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking E-commerce connection health");
            var customers = await _ecommerceService.GetCustomersAsync();
            var customerList = customers.ToList();
            
            if (customerList.Any())
            {
                _logger.LogInformation("E-commerce connection health check passed with {Count} customers", customerList.Count);
                return HealthCheckResult.Healthy($"E-commerce connection is healthy ({customerList.Count} customers)");
            }
            
            _logger.LogWarning("E-commerce connection health check - no customers found");
            return HealthCheckResult.Unhealthy("E-commerce returned no customers");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-commerce connection health check failed with exception");
            return HealthCheckResult.Unhealthy("E-commerce connection error", ex);
        }
    }
}

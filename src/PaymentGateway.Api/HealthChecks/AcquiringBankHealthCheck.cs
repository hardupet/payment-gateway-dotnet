using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PaymentGateway.Api.HealthChecks;

public sealed class AcquiringBankHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AcquiringBankHealthCheck(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BankHealthCheck");

            using var response = await client.GetAsync(
                string.Empty,
                cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded(
                    $"Acquiring bank returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Acquiring bank is unavailable.",
                ex);
        }
    }
}
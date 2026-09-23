
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Infrastructure.Bank;
using PaymentGateway.Infrastructure.Configuration;
using PaymentGateway.Infrastructure.Persistence;

namespace PaymentGateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure( this IServiceCollection services, IConfiguration configuration)
    {
        var bankOptions =
            configuration
                .GetSection(BankOptions.SectionName)
                .Get<BankOptions>()
            ?? throw new InvalidOperationException(
                "Bank configuration is missing.");

        services.AddSingleton<IPaymentsRepository, InMemoryPaymentsRepository>();

        services.AddHttpClient<IAcquiringBankClient, AcquiringBankClient>(client =>
                {
                    client.BaseAddress =
                        new Uri(bankOptions.BaseUrl);
                })
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds( bankOptions.TimeoutSeconds);

                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);

                options.Retry.MaxRetryAttempts = 2;

                options.Retry.Delay = TimeSpan.FromMilliseconds(200);

                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential; //DelayBackoffType.Exponential;

                options.Retry.UseJitter = true;

                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);

                options.CircuitBreaker.BreakDuration =TimeSpan.FromSeconds(15);

                options.CircuitBreaker.FailureRatio = 0.5;

                options.CircuitBreaker.MinimumThroughput = 5;
            });

        return services;
    }
}
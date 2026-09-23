using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Application.Services;
using PaymentGateway.Application.Validators;

namespace PaymentGateway.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication( this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddValidatorsFromAssemblyContaining<PostPaymentRequestValidator>();

        return services;
    }
}
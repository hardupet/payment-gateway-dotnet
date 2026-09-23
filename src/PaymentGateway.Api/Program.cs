

using FluentValidation.AspNetCore;

using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using PaymentGateway.Api.HealthChecks;
using PaymentGateway.Api.Middleware;
using PaymentGateway.Application;
using PaymentGateway.Infrastructure;
using PaymentGateway.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
}); 

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.Configure<BankOptions>(
    builder.Configuration.GetSection(
        BankOptions.SectionName));

builder.Services.AddHttpClient(
    "BankHealthCheck",
    client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["Bank:BaseUrl"]!);

        client.Timeout = TimeSpan.FromSeconds(2);
    });

builder.Services
    .AddHealthChecks()
    .AddCheck<AcquiringBankHealthCheck>(
        "acquiring-bank",
        tags: ["ready"]);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate =
            registration =>
                registration.Tags.Contains("ready")
    });

app.Run();

public partial class Program;

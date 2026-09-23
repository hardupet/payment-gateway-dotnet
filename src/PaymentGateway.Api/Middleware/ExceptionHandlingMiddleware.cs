using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Application.Exceptions;

namespace PaymentGateway.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (IdempotencyConflictException ex)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Idempotency conflict",
                ex.Message);
        }
        catch (PaymentAlreadyProcessingException ex)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Payment already processing",
                ex.Message);
        }
        catch (AcquiringBankUnavailableException ex)
        {
            _logger.LogWarning(
                ex,
                "Acquiring bank unavailable");

            await WriteProblemAsync(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "Acquiring bank unavailable",
                "The payment could not be processed at this time.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled application exception");

            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            });
    }
}
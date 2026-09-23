using Microsoft.Extensions.Logging;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Application.DTOs.Bank;
using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.DTOs.Responses;
using PaymentGateway.Application.Exceptions;
using PaymentGateway.Application.Helpers;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly IPaymentsRepository _repository;
    private readonly IAcquiringBankClient _bankClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IPaymentsRepository repository,IAcquiringBankClient bankClient,ILogger<PaymentService> logger)
    {
        _repository = repository;
        _bankClient = bankClient;
        _logger = logger;
    }

    public async Task<PostPaymentResponse> ProcessPaymentAsync(string idempotencyKey,PostPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var requestHash = PaymentRequestHasher.Create(request);

        var existing =  await _repository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);

        if (existing is not null)
        {
            if (!string.Equals( existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                throw new IdempotencyConflictException();
            }

            if (existing.Status == PaymentStatus.Processing)
            {
                throw new PaymentAlreadyProcessingException();
            }

            _logger.LogInformation("Returning existing payment {PaymentId} for idempotency key",existing.Id);

            return MapPostResponse(existing);
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash,
            Status = PaymentStatus.Processing,
            CardNumberLastFour = CardNumberHelper.GetLastFour(request.CardNumber),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency.ToUpperInvariant(),
            Amount = request.Amount,
            CreatedAtUtc = DateTime.UtcNow
        };

        var claimed = await _repository.TryAddAsync(payment, cancellationToken);

        if (!claimed)
        {
            // Another request won the race.
            var concurrent = await _repository.GetByIdempotencyKeyAsync( idempotencyKey,cancellationToken);

            if (concurrent is null)
            {
                throw new InvalidOperationException("Unable to resolve concurrent payment request.");
            }

            if (!string.Equals(concurrent.RequestHash, requestHash, StringComparison.Ordinal))
            {
                throw new IdempotencyConflictException();
            }

            if (concurrent.Status == PaymentStatus.Processing)
            {
                throw new PaymentAlreadyProcessingException();
            }

            return MapPostResponse(concurrent);
        }

        _logger.LogInformation( "Processing payment {PaymentId}", payment.Id);

        try
        {
            var bankRequest = new BankPaymentRequest
            {
                CardNumber = request.CardNumber,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency.ToUpperInvariant(),
                Amount = request.Amount,
                Cvv = request.Cvv
            };

            var bankResponse = await _bankClient.ProcessPaymentAsync(bankRequest, idempotencyKey,cancellationToken);

            payment.Status = bankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined;

            payment.AuthorizationCode = bankResponse.AuthorizationCode;

            payment.CompletedAtUtc = DateTime.UtcNow;

            await _repository.UpdateAsync( payment,cancellationToken);

            _logger.LogInformation("Payment {PaymentId} completed with status {Status}",   payment.Id, payment.Status);

            return MapPostResponse(payment);
        }
        catch (Exception ex)
        {
            payment.Status = PaymentStatus.Failed;
            payment.CompletedAtUtc = DateTime.UtcNow;

            await _repository.UpdateAsync( payment, cancellationToken);

            _logger.LogError( ex,"Payment {PaymentId} failed while communicating with acquiring bank",payment.Id);

            throw;
        }
    }

    public async Task<GetPaymentResponse?> GetPaymentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await _repository.GetByIdAsync(id,cancellationToken);

        return payment is null ? null : new GetPaymentResponse
            {
                Id = payment.Id,
                Status = payment.Status,
                CardNumberLastFour = payment.CardNumberLastFour,
                ExpiryMonth = payment.ExpiryMonth,
                ExpiryYear = payment.ExpiryYear,
                Currency = payment.Currency,
                Amount = payment.Amount
            };
    }

    private static PostPaymentResponse MapPostResponse( Payment payment)
    {
        return new PostPaymentResponse
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumberLastFour = payment.CardNumberLastFour,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount
        };
    }
}
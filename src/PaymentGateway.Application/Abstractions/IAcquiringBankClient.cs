using PaymentGateway.Application.DTOs.Bank;

namespace PaymentGateway.Application.Abstractions;

public interface IAcquiringBankClient
{
    Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request,string idempotencyKey,CancellationToken cancellationToken = default);
}
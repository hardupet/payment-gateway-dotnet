
using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.DTOs.Responses;

namespace PaymentGateway.Application.Abstractions;

public interface IPaymentService
{
    Task<PostPaymentResponse> ProcessPaymentAsync(string idempotencyKey, PostPaymentRequest request, CancellationToken cancellationToken = default);

    Task<GetPaymentResponse?> GetPaymentAsync( Guid id, CancellationToken cancellationToken = default);
}
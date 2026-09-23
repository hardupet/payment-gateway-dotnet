using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Abstractions;

public interface IPaymentsRepository
{
    Task<Payment?> GetByIdAsync(Guid id,CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey,CancellationToken cancellationToken = default);

    Task<bool> TryAddAsync(Payment payment, CancellationToken cancellationToken = default);

    Task UpdateAsync( Payment payment, CancellationToken cancellationToken = default);
}
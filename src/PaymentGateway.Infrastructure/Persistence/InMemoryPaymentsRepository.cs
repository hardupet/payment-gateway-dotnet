using System.Collections.Concurrent;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Infrastructure.Persistence;

public sealed class InMemoryPaymentsRepository : IPaymentsRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _paymentsById = new();

    private readonly ConcurrentDictionary<string, Guid> _paymentIdsByIdempotencyKey = new(StringComparer.Ordinal);

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _paymentsById.TryGetValue(id, out var payment);

        return Task.FromResult(payment);
    }

    public Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey,CancellationToken cancellationToken = default)
    {
        if (!_paymentIdsByIdempotencyKey.TryGetValue(idempotencyKey,out var paymentId))
        {
            return Task.FromResult<Payment?>(null);
        }

        _paymentsById.TryGetValue(paymentId,out var payment);

        return Task.FromResult(payment);
    }

    public Task<bool> TryAddAsync( Payment payment,CancellationToken cancellationToken = default)
    {
        if (!_paymentIdsByIdempotencyKey.TryAdd(payment.IdempotencyKey,payment.Id))
        {
            return Task.FromResult(false);
        }

        if (!_paymentsById.TryAdd( payment.Id, payment))
        {
            _paymentIdsByIdempotencyKey.TryRemove(payment.IdempotencyKey,out _);

            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task UpdateAsync(Payment payment,CancellationToken cancellationToken = default)
    {
        _paymentsById[payment.Id] = payment;

        return Task.CompletedTask;
    }
}
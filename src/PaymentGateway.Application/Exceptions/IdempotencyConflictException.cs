namespace PaymentGateway.Application.Exceptions;

public sealed class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException() : base("The idempotency key has already been used for a different payment request.")
    {
    }
}
namespace PaymentGateway.Application.Exceptions;

public sealed class AcquiringBankUnavailableException : Exception
{
    public AcquiringBankUnavailableException( string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}
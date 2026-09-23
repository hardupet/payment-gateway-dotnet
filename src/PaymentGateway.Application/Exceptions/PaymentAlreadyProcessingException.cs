namespace PaymentGateway.Application.Exceptions;

public sealed class PaymentAlreadyProcessingException : Exception
{
    public PaymentAlreadyProcessingException() : base("This payment request is already being processed.")
    {
    }
}
namespace PaymentGateway.Application.DTOs.Bank;

public sealed class BankPaymentResponse
{
    public bool Authorized { get; init; }

    public string? AuthorizationCode { get; init; }
}
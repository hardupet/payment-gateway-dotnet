namespace PaymentGateway.Domain.Entities;

public enum PaymentStatus
{
    Processing,
    Authorized,
    Declined,
    Rejected,
    Failed
}
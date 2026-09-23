namespace PaymentGateway.Infrastructure.Configuration;

public class BankOptions
{
    public const string SectionName = "Bank";

    public string BaseUrl { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 5;
}
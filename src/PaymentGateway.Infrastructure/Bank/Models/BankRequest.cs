using System.Text.Json.Serialization;

namespace PaymentGateway.Infrastructure.Bank.Models;

internal class BankRequest
{
    [JsonPropertyName("card_number")]
    public string CardNumber { get; init; } = string.Empty;

    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; init; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public int Amount { get; init; }

    [JsonPropertyName("cvv")]
    public string Cvv { get; init; } = string.Empty;
}
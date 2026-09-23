using System.Security.Cryptography;
using System.Text;

using PaymentGateway.Application.DTOs.Requests;


namespace PaymentGateway.Application.Helpers;

public static class PaymentRequestHasher
{
    public static string Create(PostPaymentRequest request)
    {
        var normalized =
            $"{request.CardNumber}|" +
            $"{request.ExpiryMonth}|" +
            $"{request.ExpiryYear}|" +
            $"{request.Currency.ToUpperInvariant()}|" +
            $"{request.Amount}";

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));

        return Convert.ToHexString(bytes);
    }
}
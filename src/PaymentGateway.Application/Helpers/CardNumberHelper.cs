namespace PaymentGateway.Application.Helpers;

public static class CardNumberHelper
{
    public static string GetLastFour(string cardNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardNumber);

        if (cardNumber.Length < 4)
        {
            throw new ArgumentException(
                "Card number must contain at least four digits.",
                nameof(cardNumber));
        }

        return cardNumber[^4..];
    }
}
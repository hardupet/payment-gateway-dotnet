using FluentValidation;

using PaymentGateway.Application.DTOs.Requests;

namespace PaymentGateway.Application.Validators;

public class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
{
    private readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "GBP",
            "USD",
            "EUR"
        };


    public PostPaymentRequestValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .WithMessage("Card number is required.")
            .Matches(@"^\d{14,19}$")
            .WithMessage("Card number must contain between 14 and 19 digits.");

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12)
            .WithMessage("Expiry month must be between 1 and 12.");

        RuleFor(x => x.ExpiryYear)
            .Must((request, expiryYear) =>
            {
                // Don't duplicate the month validation error here.
                if (request.ExpiryMonth is < 1 or > 12)
                {
                    return true;
                }

                var now = DateTime.UtcNow;

                return expiryYear > now.Year ||
                       (expiryYear == now.Year &&
                        request.ExpiryMonth >= now.Month);
            })
            .WithMessage("Card has expired.");

        RuleFor(x => x.Currency)
          .NotEmpty()
          .WithMessage("Currency is required.")
          .Matches(@"^[A-Za-z]{3}$")
          .WithMessage("Currency must be a valid three-letter currency code.");

        RuleFor(x => x.Currency)
            .Must(currency => SupportedCurrencies.Contains(currency))
            .When(x =>
                !string.IsNullOrWhiteSpace(x.Currency) &&
                x.Currency.Length == 3)
            .WithMessage("Currency is not supported.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Cvv)
            .NotEmpty()
            .WithMessage("CVV is required.")
            .Matches(@"^\d{3,4}$")
            .WithMessage("CVV must contain 3 or 4 digits.");
    }


    private static bool HaveValidExpiryDate(PostPaymentRequest request)
    {
        if (request.ExpiryMonth is < 1 or > 12)
        {
            return false;
        }

        var now = DateTime.UtcNow;

        return request.ExpiryYear > now.Year || request.ExpiryYear == now.Year &&  request.ExpiryMonth >= now.Month;
    }
}
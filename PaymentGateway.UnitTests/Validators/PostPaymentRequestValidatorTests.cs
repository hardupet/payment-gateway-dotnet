
using FluentValidation.TestHelper;

using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.Validators;

namespace PaymentGateway.UnitTests.Validators;

public sealed class PostPaymentRequestValidatorTests
{
    private readonly PostPaymentRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldNotHaveValidationErrors()
    {
        var request = CreateRequest();

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("1234567890123")]        // 13 digits
    [InlineData("12345678901234567890")] // 20 digits
    [InlineData("22224053432488AB")]
    public void Validate_InvalidCardNumber_ShouldHaveValidationError(
        string cardNumber)
    {
        var request = CreateRequest(cardNumber: cardNumber);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CardNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_InvalidExpiryMonth_ShouldHaveValidationError(
        int expiryMonth)
    {
        var request = CreateRequest(expiryMonth: expiryMonth);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth);
    }

    [Fact]
    public void Validate_ExpiredCard_ShouldHaveValidationError()
    {
        // Arrange
        var request = CreateRequest(
            expiryMonth: 1,
            expiryYear: DateTime.UtcNow.Year - 1);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExpiryYear);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Validate_InvalidCurrencyFormat_ShouldHaveValidationError(
      string currency)
    {
        // Arrange
        var request = CreateRequest(currency: currency);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("NGN")]
    [InlineData("CAD")]
    [InlineData("JPY")]
    public void Validate_UnsupportedCurrency_ShouldHaveValidationError(
        string currency)
    {
        // Arrange
        var request = CreateRequest(currency: currency);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_NonPositiveAmount_ShouldHaveValidationError(
        int amount)
    {
        var request = CreateRequest(amount: amount);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("12A")]
    [InlineData("ABCD")]
    public void Validate_InvalidCvv_ShouldHaveValidationError(
        string cvv)
    {
        var request = CreateRequest(cvv: cvv);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Cvv);
    }

    private static PostPaymentRequest CreateRequest(
        string cardNumber = "2222405343248877",
        int expiryMonth = 9,
        int expiryYear = 2027,
        string currency = "GBP",
        int amount = 6000,
        string cvv = "567")
    {
        return new PostPaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = expiryMonth,
            ExpiryYear = expiryYear,
            Currency = currency,
            Amount = amount,
            Cvv = cvv
        };
    }
}
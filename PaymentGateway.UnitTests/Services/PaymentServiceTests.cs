using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Application.DTOs.Bank;
using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.Exceptions;
using PaymentGateway.Application.Services;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.UnitTests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentsRepository> _repositoryMock;
    private readonly Mock<IAcquiringBankClient> _bankClientMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;

    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _repositoryMock = new Mock<IPaymentsRepository>();
        _bankClientMock = new Mock<IAcquiringBankClient>();
        _loggerMock = new Mock<ILogger<PaymentService>>();

        _sut = new PaymentService(
            _repositoryMock.Object,
            _bankClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_AuthorizedBankResponse_ShouldReturnAuthorized()
    {
        // Arrange
        var request = CreateValidRequest();

        _repositoryMock.Setup(x => x.GetByIdempotencyKeyAsync( It.IsAny<string>(),It.IsAny<CancellationToken>())).ReturnsAsync((Payment?)null);

        _repositoryMock
            .Setup(x => x.TryAddAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.UpdateAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(
                It.IsAny<BankPaymentRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankPaymentResponse
            {
                Authorized = true,
                AuthorizationCode = "AUTH-123"
            });

        // Act
        var result = await _sut.ProcessPaymentAsync(
            "authorized-001",
            request);

        // Assert
        result.Status.Should().Be(PaymentStatus.Authorized);
        result.CardNumberLastFour.Should().Be("8877");
        result.Amount.Should().Be(6000);
        result.Currency.Should().Be("GBP");

        _bankClientMock.Verify(
            x => x.ProcessPaymentAsync(
                It.IsAny<BankPaymentRequest>(),
                "authorized-001",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_UnauthorizedBankResponse_ShouldReturnDeclined()
    {
        // Arrange
        var request = CreateValidRequest(
            cardNumber: "2222405343248878");

        _repositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        _repositoryMock
            .Setup(x => x.TryAddAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.UpdateAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(
                It.IsAny<BankPaymentRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankPaymentResponse
            {
                Authorized = false,
                AuthorizationCode = null
            });

        // Act
        var result = await _sut.ProcessPaymentAsync(
            "declined-001",
            request);

        // Assert
        result.Status.Should().Be(PaymentStatus.Declined);
        result.CardNumberLastFour.Should().Be("8878");

        _bankClientMock.Verify(
            x => x.ProcessPaymentAsync(
                It.IsAny<BankPaymentRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SameIdempotencyKeyAndSameRequest_ShouldReturnExistingPayment()
    {
        // Arrange
        var request = CreateValidRequest();

        var requestHash =
            PaymentGateway.Application.Helpers.PaymentRequestHasher
                .Create(request);

        var existingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "payment-001",
            RequestHash = requestHash,
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = "8877",
            ExpiryMonth = 9,
            ExpiryYear = 2027,
            Currency = "GBP",
            Amount = 6000,
            AuthorizationCode = "AUTH-123",
            CreatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(
                "payment-001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);

        // Act
        var result = await _sut.ProcessPaymentAsync(
            "payment-001",
            request);

        // Assert
        result.Id.Should().Be(existingPayment.Id);
        result.Status.Should().Be(PaymentStatus.Authorized);

        // Most important idempotency assertion:
        // the acquiring bank must NOT be called again.
        _bankClientMock.Verify(
            x => x.ProcessPaymentAsync(
                It.IsAny<BankPaymentRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repositoryMock.Verify(
            x => x.TryAddAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SameIdempotencyKeyAndDifferentRequest_ShouldThrowConflict()
    {
        // Arrange
        var originalRequest = CreateValidRequest();

        var originalHash =
            PaymentGateway.Application.Helpers.PaymentRequestHasher
                .Create(originalRequest);

        var existingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "payment-001",
            RequestHash = originalHash,
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = "8877",
            ExpiryMonth = 9,
            ExpiryYear = 2027,
            Currency = "GBP",
            Amount = 6000,
            CreatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(
                "payment-001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);

        var differentRequest = CreateValidRequest(amount: 7000);

        // Act
        Func<Task> act = () => _sut.ProcessPaymentAsync( "payment-001", differentRequest);

        // Assert
        await act.Should()
            .ThrowAsync<IdempotencyConflictException>();

        _bankClientMock.Verify(
            x => x.ProcessPaymentAsync(
                It.IsAny<BankPaymentRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldSendExpectedRequestToBank()
    {
        var request = CreateValidRequest();

        _repositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        _repositoryMock
            .Setup(x => x.TryAddAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.UpdateAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(
                It.IsAny<BankPaymentRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankPaymentResponse
            {
                Authorized = true,
                AuthorizationCode = "AUTH-123"
            });

        await _sut.ProcessPaymentAsync(
            "payment-001",
            request);

        _bankClientMock.Verify(
            x => x.ProcessPaymentAsync(
                It.Is<BankPaymentRequest>(bankRequest =>
                    bankRequest.CardNumber == request.CardNumber &&
                    bankRequest.ExpiryMonth == request.ExpiryMonth &&
                    bankRequest.ExpiryYear == request.ExpiryYear &&
                    bankRequest.Currency == "GBP" &&
                    bankRequest.Amount == 6000 &&
                    bankRequest.Cvv == request.Cvv),
                "payment-001",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static PostPaymentRequest CreateValidRequest(
    string cardNumber = "2222405343248877",
    int amount = 6000)
    {
        return new PostPaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = 9,
            ExpiryYear = 2027,
            Currency = "GBP",
            Amount = amount,
            Cvv = "567"
        };
    }
}
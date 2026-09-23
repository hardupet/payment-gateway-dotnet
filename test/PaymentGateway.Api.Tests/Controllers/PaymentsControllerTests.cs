using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Application.Abstractions;
using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.DTOs.Responses;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Api.Tests.Controllers;

public sealed class PaymentsControllerTests
{
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _paymentServiceMock = new Mock<IPaymentService>();

        _controller = new PaymentsController(_paymentServiceMock.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidPayment_ShouldReturnOk()
    {
        // Arrange
        var request = CreateValidRequest();

        var response = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = "8877",
            ExpiryMonth = 9,
            ExpiryYear = 2027,
            Currency = "GBP",
            Amount = 6000
        };

        _paymentServiceMock
            .Setup(x => x.ProcessPaymentAsync(
                "payment-001",
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result =
            await _controller.ProcessPaymentAsync(
                "payment-001",
                request,
                CancellationToken.None);

        // Assert
        var okResult = result.Result
            .Should()
            .BeOfType<OkObjectResult>()
            .Subject;

        okResult.StatusCode.Should().Be(200);

        var body = okResult.Value
            .Should()
            .BeOfType<PostPaymentResponse>()
            .Subject;

        body.Id.Should().Be(response.Id);
        body.Status.Should().Be(PaymentStatus.Authorized);
        body.CardNumberLastFour.Should().Be("8877");
    }

    [Fact]
    public async Task GetPaymentAsync_ExistingPayment_ShouldReturnOk()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        var response = new GetPaymentResponse
        {
            Id = paymentId,
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = "8877",
            ExpiryMonth = 9,
            ExpiryYear = 2027,
            Currency = "GBP",
            Amount = 6000
        };

        _paymentServiceMock
            .Setup(x => x.GetPaymentAsync(
                paymentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result =
            await _controller.GetPaymentAsync(
                paymentId,
                CancellationToken.None);

        // Assert
        var okResult = result.Result
            .Should()
            .BeOfType<OkObjectResult>()
            .Subject;

        okResult.StatusCode.Should().Be(200);

        var body = okResult.Value
            .Should()
            .BeOfType<GetPaymentResponse>()
            .Subject;

        body.Id.Should().Be(paymentId);
        body.Status.Should().Be(PaymentStatus.Authorized);
    }

    [Fact]
    public async Task GetPaymentAsync_MissingPayment_ShouldReturnNotFound()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        _paymentServiceMock
            .Setup(x => x.GetPaymentAsync(
                paymentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetPaymentResponse?)null);

        // Act
        var result =
            await _controller.GetPaymentAsync(
                paymentId,
                CancellationToken.None);

        // Assert
        result.Result
            .Should()
            .BeOfType<NotFoundResult>();
    }

    private static PostPaymentRequest CreateValidRequest()
    {
        return new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 9,
            ExpiryYear = 2027,
            Currency = "GBP",
            Amount = 6000,
            Cvv = "567"
        };
    }
}
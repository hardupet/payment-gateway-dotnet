using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

using PaymentGateway.Application.Abstractions;
using PaymentGateway.Application.DTOs.Bank;
using PaymentGateway.Application.Exceptions;
using PaymentGateway.Infrastructure.Bank.Models;

namespace PaymentGateway.Infrastructure.Bank;

public sealed class AcquiringBankClient : IAcquiringBankClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AcquiringBankClient> _logger;

    public AcquiringBankClient(
        HttpClient httpClient,
        ILogger<AcquiringBankClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BankPaymentResponse> ProcessPaymentAsync(
        BankPaymentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var bankRequest = new BankRequest
        {
            CardNumber = request.CardNumber,
            ExpiryDate =
                $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
            Currency = request.Currency,
            Amount = request.Amount,
            Cvv = request.Cvv
        };

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            "payments")
        {
            Content = JsonContent.Create(bankRequest)
        };

        _logger.LogInformation(
            "Sending payment request to acquiring bank");

        using var response = await _httpClient.SendAsync(
            message,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new AcquiringBankUnavailableException(
                $"Acquiring bank returned HTTP " +
                $"{(int)response.StatusCode}.");
        }

        var result =
            await response.Content.ReadFromJsonAsync<BankResponse>(
                cancellationToken: cancellationToken);

        if (result is null)
        {
            throw new AcquiringBankUnavailableException(
                "Acquiring bank returned an empty response.");
        }

        return new BankPaymentResponse
        {
            Authorized = result.Authorized,
            AuthorizationCode = result.AuthorizationCode
        };
    }
}
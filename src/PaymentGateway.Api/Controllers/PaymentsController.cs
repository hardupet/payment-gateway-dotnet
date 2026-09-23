using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Application.Abstractions;
using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.DTOs.Responses;

namespace PaymentGateway.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse),StatusCodes.Status200OK)]
    [ProducesResponseType( StatusCodes.Status400BadRequest)]
    [ProducesResponseType( StatusCodes.Status409Conflict)]
    [ProducesResponseType( StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PostPaymentResponse>> ProcessPaymentAsync( [FromHeader(Name = "Idempotency-Key")]string idempotencyKey,
                                                                              [FromBody] PostPaymentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Missing idempotency key",
                    Detail =
                        "Idempotency-Key header is required.",
                    Status =
                        StatusCodes.Status400BadRequest
                });
        }

        if (idempotencyKey.Length > 100)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid idempotency key",
                    Detail = "Idempotency-Key must not exceed 100 characters.",
                    Status = StatusCodes.Status400BadRequest
                });
        }

        var result = await _paymentService.ProcessPaymentAsync(idempotencyKey,request,cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetPaymentResponse>>
        GetPaymentAsync( Guid id,CancellationToken cancellationToken)
    {
        var payment =  await _paymentService.GetPaymentAsync(id,cancellationToken);

        return payment is null ? NotFound(): Ok(payment);
    }
}
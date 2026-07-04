using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPayment.Api.Models;
using MiniPayment.Application.Features.Payments.ProcessPayment;
using Swashbuckle.AspNetCore.Annotations;

namespace MiniPayment.Api.Controllers.V1;

/// <summary>Payment processing endpoint.</summary>
[ApiController]
[Route("api/v1")]
[Authorize]
[Produces("application/json")]
public class PaymentsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Process a payment transaction.
    /// </summary>
    /// <remarks>
    /// The approval decision is based on the decimal portion of the amount:
    /// - .00 → APPROVED (response_code: "00")
    /// - Other → DECLINED (response_code: last two decimal digits)
    /// </remarks>
    [HttpPost("pay")]
    [SwaggerOperation(Summary = "Process payment", Tags = ["Payments"])]
    [SwaggerResponse(200, "Payment processed (check status field)", typeof(PayResponse))]
    [SwaggerResponse(401, "Unauthorized — missing or invalid Bearer token")]
    [SwaggerResponse(422, "Validation error")]
    public async Task<ActionResult<PayResponse>> Pay(
        [FromBody] PayRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ProcessPaymentCommand
        {
            OrderNumber = request.OrderNumber,
            CardNumber = request.CardNumber,
            ExpiryDate = request.ExpiryDate,
            Cvv = request.Cvv,
            Currency = request.Currency,
            CardholderName = request.CardholderName,
            Email = request.Email,
            Amount = request.Amount
        };

        var result = await mediator.Send(command, cancellationToken);

        return Ok(new PayResponse
        {
            TransactionId = result.TransactionId,
            AcquirerReference = result.AcquirerReference,
            ResponseCode = result.ResponseCode,
            Status = result.Status.ToString().ToUpper(),
            Timestamp = result.Timestamp,
            Amount = result.Amount,
            Message = result.Message
        });
    }
}

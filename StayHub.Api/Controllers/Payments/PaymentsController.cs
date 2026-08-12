using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayHub.Api.Extensions;
using StayHub.Application.Payments.GetPaymentByBooking;
using StayHub.Application.Payments.InitiatePayment;
using StayHub.Application.Payments.MarkPaymentFailed;
using StayHub.Application.Payments.MarkPaymentSucceeded;
using StayHub.Application.Payments.RefundPayment;
using StayHub.Infrastructure.Authorization;
using StayHub.Infrastructure.Payments;
using Stripe;

namespace StayHub.Api.Controllers.Payments;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}/payments")]
public sealed class PaymentsController(ISender sender, StripeWebhookEventParser webhookEventParser) : ControllerBase
{
    [HttpGet("by-booking/{bookingId:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse>> GetByBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        var query = new GetPaymentByBookingQuery(bookingId);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpPost]
    [HasPermission(Permissions.PaymentCreate)]
    [ProducesResponseType(typeof(InitiatePaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InitiatePaymentResponse>> Initiate(
        [FromBody] InitiatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new InitiatePaymentCommand(request.BookingId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(this)
            : CreatedAtAction(nameof(GetByBooking), new { request.BookingId }, result.Value);
    }

    [HttpPost("{paymentId:guid}/refund")]
    [HasPermission(Permissions.PaymentRefund)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Refund(Guid paymentId, CancellationToken cancellationToken)
    {
        var command = new RefundPaymentCommand(paymentId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    /// <summary>
    /// Called by Stripe, never by a client. Verifies the signature before trusting the payload -
    /// see StripeWebhookEventParser and the setup guide for why this check is mandatory.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("Stripe-Signature", out var signatureHeader))
        {
            return BadRequest();
        }

        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

        Event stripeEvent;

        try
        {
            stripeEvent = webhookEventParser.ConstructEvent(requestBody, signatureHeader!);
        }
        catch (StripeException)
        {
            return BadRequest();
        }

        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
            {
                var paymentIntent = (PaymentIntent)stripeEvent.Data.Object;
                await sender.Send(new MarkPaymentSucceededCommand(paymentIntent.Id), cancellationToken);
                break;
            }
            case "payment_intent.payment_failed":
            {
                var paymentIntent = (PaymentIntent)stripeEvent.Data.Object;
                await sender.Send(new MarkPaymentFailedCommand(paymentIntent.Id), cancellationToken);
                break;
            }
        }

        return Ok();
    }
}
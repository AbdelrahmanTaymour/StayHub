using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayHub.Api.Extensions;
using StayHub.Application.Payments.GetPaymentByBooking;
using StayHub.Application.Payments.InitiatePayment;
using StayHub.Application.Payments.MarkPaymentFailed;
using StayHub.Application.Payments.MarkPaymentSucceeded;
using StayHub.Application.Payments.RefundPayment;
using StayHub.Infrastructure.Payments;
using Stripe;

namespace StayHub.Api.Controllers.Payments;

[ApiController]
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
    [ProducesResponseType(typeof(InitiatePaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InitiatePaymentResponse>> Initiate(
        [FromBody] Guid bookingId,
        CancellationToken cancellationToken)
    {
        var command = new InitiatePaymentCommand(bookingId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(this)
            : CreatedAtAction(nameof(GetByBooking), new { bookingId }, result.Value);
    }

    [HttpPost("{paymentId:guid}/refund")]
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
    ///     Called by Stripe, never by a client. Verifies the signature before trusting the payload -
    ///     see StripeWebhookEventParser and the setup guide for why this check is mandatory.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

        Event stripeEvent;

        try
        {
            stripeEvent = webhookEventParser.ConstructEvent(requestBody, Request.Headers["Stripe-Signature"]!);
        }
        catch (StripeException)
        {
            // Signature verification failed - this request did not genuinely come from Stripe.
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
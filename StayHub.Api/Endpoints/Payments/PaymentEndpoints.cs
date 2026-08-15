using MediatR;
using StayHub.Api.Extensions;
using StayHub.Application.Payments.GetPaymentByBooking;
using StayHub.Application.Payments.InitiatePayment;
using StayHub.Application.Payments.MarkPaymentFailed;
using StayHub.Application.Payments.MarkPaymentSucceeded;
using StayHub.Application.Payments.RefundPayment;
using StayHub.Infrastructure.Payments;
using Stripe;

namespace StayHub.Api.Endpoints.Payments;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("payments").WithTags("Payments").RequireAuthorization();

        // No .HasPermission — caller is either the booking's guest or the
        // apartment's host; enforce in GetPaymentByBookingQueryHandler.
        group.MapGet("by-booking/{bookingId:guid}", GetByBooking)
            .WithName(nameof(GetByBooking))
            .Produces<PaymentResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("", Initiate)
            .HasPermission(Permissions.PaymentCreate)
            .Produces<InitiatePaymentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("{paymentId:guid}/refund", Refund)
            .HasPermission(Permissions.PaymentRefund)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Called by Stripe, never by a client. Verifies the signature before
        // trusting the payload - see StripeWebhookEventParser.
        group.MapPost("webhook", Webhook)
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return builder;
    }

    private static async Task<IResult> GetByBooking(
        Guid bookingId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentByBookingQuery(bookingId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> Initiate(
        InitiatePaymentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new InitiatePaymentCommand(request.BookingId), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : TypedResults.CreatedAtRoute(
                result.Value,
                nameof(GetByBooking),
                new { bookingId = request.BookingId });
    }

    private static async Task<IResult> Refund(Guid paymentId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefundPaymentCommand(paymentId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> Webhook(
        HttpRequest request,
        ISender sender,
        StripeWebhookEventParser webhookEventParser,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.TryGetValue("Stripe-Signature", out var signatureHeader))
        {
            return Results.BadRequest();
        }

        var requestBody = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);

        Event stripeEvent;

        try
        {
            stripeEvent = webhookEventParser.ConstructEvent(requestBody, signatureHeader!);
        }
        catch (StripeException)
        {
            return Results.BadRequest();
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

        return Results.Ok();
    }
}
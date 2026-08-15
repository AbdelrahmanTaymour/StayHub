using MediatR;
using StayHub.Api.Extensions;
using StayHub.Application.Bookings.CancelBooking;
using StayHub.Application.Bookings.CompleteBooking;
using StayHub.Application.Bookings.ConfirmBooking;
using StayHub.Application.Bookings.GetBooking;
using StayHub.Application.Bookings.GetBookingsByApartment;
using StayHub.Application.Bookings.GetBookingsByUser;
using StayHub.Application.Bookings.GetMyBookings;
using StayHub.Application.Bookings.RejectBooking;
using StayHub.Application.Bookings.ReserveBooking;

namespace StayHub.Api.Endpoints.Bookings;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("bookings").WithTags("Bookings").RequireAuthorization();

        group.MapGet("{id:guid}", GetBooking)
            .WithName(nameof(GetBooking))
            .Produces<BookingResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("mine", GetMine)
            .Produces<IReadOnlyList<BookingSummaryResponse>>();

        group.MapGet("by-user/{userId:guid}", GetByUser)
            .HasPermission(Permissions.BookingManage)
            .Produces<IReadOnlyList<BookingSummaryResponse>>();

        group.MapGet("by-apartment/{apartmentId:guid}", GetByApartment)
            .HasPermission(Permissions.BookingManage)
            .Produces<IReadOnlyList<BookingSummaryResponse>>();

        group.MapPost("", Reserve)
            .HasPermission(Permissions.BookingCreate)
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("{id:guid}/confirm", Confirm)
            .HasPermission(Permissions.BookingManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("{id:guid}/reject", Reject)
            .HasPermission(Permissions.BookingManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // No .HasPermission — both the guest (own stay) and the host can
        // reach this; that OR-logic lives in CancelBookingCommandHandler via
        // IUserContext.
        group.MapPost("{id:guid}/cancel", Cancel)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return builder;
    }

    private static async Task<IResult> GetBooking(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBookingQuery(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> GetMine(
        ISender sender,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetMyBookingsQuery(page, pageSize), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> GetByUser(
        Guid userId,
        ISender sender,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetBookingsByUserQuery(userId, page, pageSize), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> GetByApartment(
        Guid apartmentId,
        ISender sender,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetBookingsByApartmentQuery(apartmentId, page, pageSize), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> Reserve(
        ReserveBookingRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ReserveBookingCommand(request.ApartmentId, request.StartDate, request.EndDate);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : TypedResults.CreatedAtRoute(result.Value, nameof(GetBooking), new { id = result.Value });
    }

    private static async Task<IResult> Confirm(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmBookingCommand(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.NoContent();
    }

    private static async Task<IResult> Reject(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RejectBookingCommand(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.NoContent();
    }

    private static async Task<IResult> Cancel(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelBookingCommand(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.NoContent();
    }

    private static async Task<IResult> Complete(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CompleteBookingCommand(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.NoContent();
    }
}
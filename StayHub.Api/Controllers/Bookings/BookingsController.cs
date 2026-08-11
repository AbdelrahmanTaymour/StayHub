using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
using StayHub.Infrastructure.Authorization;

namespace StayHub.Api.Controllers.Bookings;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}/bookings")]
public sealed class BookingsController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> GetBooking(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetBookingQuery(id);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    // No [HasPermission] — this always resolves to the caller's own bookings
    // (GetMyBookingsQuery takes no userId), so it's self-scoped by construction.
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookingSummaryResponse>>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMyBookingsQuery(page, pageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpGet("by-user/{userId:guid}")]
    [HasPermission(Permissions.BookingManage)]
    [ProducesResponseType(typeof(IReadOnlyList<BookingSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookingSummaryResponse>>> GetByUser(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBookingsByUserQuery(userId, page, pageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    // Host viewing bookings in their own apartment. BookingManage gates who
    // can completely call this; the handler will verify the caller
    // owns/staffs this specific apartmentId.
    [HttpGet("by-apartment/{apartmentId:guid}")]
    [HasPermission(Permissions.BookingManage)]
    [ProducesResponseType(typeof(IReadOnlyList<BookingSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookingSummaryResponse>>> GetByApartment(
        Guid apartmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBookingsByApartmentQuery(apartmentId, page, pageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpPost]
    [HasPermission(Permissions.BookingCreate)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Reserve(ReserveBookingRequest request, CancellationToken cancellationToken)
    {
        var command = new ReserveBookingCommand(request.ApartmentId, request.StartDate, request.EndDate);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(this)
            : CreatedAtAction(nameof(GetBooking), new { id = result.Value }, result.Value);
    }

    // Host-only action — a guest doesn't confirm their own booking.
    [HttpPost("{id:guid}/confirm")]
    [HasPermission(Permissions.BookingManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var command = new ConfirmBookingCommand(id);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    // Host-only action — rejecting a pending request, distinct from a guest cancelling.
    [HttpPost("{id:guid}/reject")]
    [HasPermission(Permissions.BookingManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        var command = new RejectBookingCommand(id);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    // Intentionally NOT gated by [HasPermission]: both the guest (cancelling
    // their own stay) and the host (cancelling on their apartment) can reach
    // this, and a single permission attribute can't express "self OR manage".
    // That OR-logic belongs in CancelBookingCommandHandler via IUserContext.
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var command = new CancelBookingCommand(id);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }


    // TODO: Remove this endpoint after testing
    // Complete is deliberately NOT exposed here. Nobody legitimately "marks a booking
    // complete" by clicking a button - it happens automatically once the stay's end date passes,
    // via CompleteExpiredBookingsJob (Hangfire), which sends CompleteBookingCommand directly through
    // MediatR without going through this controller at all. Exposing this as a public endpoint would
    // let any authenticated user mark any booking complete on demand, with no legitimate use case.
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var command = new CompleteBookingCommand(id);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }
}
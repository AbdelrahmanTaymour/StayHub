using System.Text;
using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Payments;

namespace StayHub.Application.Bookings.GetApartmentBookings;

internal sealed class GetApartmentBookingsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    IFileStorageService fileStorageService)
    : IQueryHandler<GetApartmentBookingsQuery, PagedResponse<ApartmentBookingResponse>>
{
    public async Task<Result<PagedResponse<ApartmentBookingResponse>>> Handle(
        GetApartmentBookingsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch { < 1 => 10, > 50 => 50, _ => request.PageSize };

        using var connection = sqlConnectionFactory.CreateConnection();

        var ownerId = await connection.ExecuteScalarAsync<Guid?>(
            "SELECT owner_id FROM apartments WHERE id = @ApartmentId", new { request.ApartmentId });

        if (ownerId is null)
        {
            return Result.Failure<PagedResponse<ApartmentBookingResponse>>(ApartmentErrors.NotFound);
        }

        if (!userContext.IsOwner(ownerId.Value) && !userContext.IsAdmin)
        {
            return Result.Failure<PagedResponse<ApartmentBookingResponse>>(ApartmentErrors.NotAuthorized);
        }

        var hasSearch = !string.IsNullOrWhiteSpace(request.Search);

        var statusFilter = request.Filter switch
        {
            ApartmentBookingsFilter.Pending => "AND b.status = @PendingStatus",
            ApartmentBookingsFilter.Confirmed => "AND b.status = @ConfirmedStatus",
            ApartmentBookingsFilter.Cancelled => "AND b.status = @CancelledStatus",
            ApartmentBookingsFilter.Rejected => "AND b.status = @RejectedStatus",
            _ => string.Empty
        };

        var searchFilter = hasSearch
            ? "AND (u.first_name || ' ' || u.last_name ILIKE @Search OR b.id::text ILIKE @Search)"
            : string.Empty;

        var orderBy = request.Sort switch
        {
            ApartmentBookingsSort.CheckInDesc => "b.duration_start DESC, b.created_on_utc DESC",
            ApartmentBookingsSort.TotalDesc => "b.total_price_amount DESC, b.created_on_utc DESC",
            ApartmentBookingsSort.TotalAsc => "b.total_price_amount ASC, b.created_on_utc DESC",
            _ => "b.duration_start ASC, b.created_on_utc DESC"
        };

        var sql = new StringBuilder($"""
                                     SELECT
                                         b.id AS Id,
                                         u.id AS GuestId,
                                         u.first_name || ' ' || u.last_name AS GuestFullName,
                                         profile.avatar_key AS GuestAvatarKey,
                                         b.status AS Status,
                                         b.duration_start AS DurationStart,
                                         b.duration_end AS DurationEnd,
                                         b.total_price_amount AS TotalPriceAmount,
                                         b.total_price_currency AS TotalPriceCurrency,
                                         b.created_on_utc AS CreatedOnUtc,
                                         COUNT(*) OVER() AS TotalCount,
                                         payment.status AS PaymentStatus

                                     FROM bookings b

                                     JOIN users u
                                         ON u.id = b.user_id

                                     LEFT JOIN user_profiles profile
                                         ON profile.user_id = u.id

                                     LEFT JOIN LATERAL (
                                         SELECT pay.status
                                         FROM payments pay
                                         WHERE pay.booking_id = b.id
                                         ORDER BY pay.created_on_utc DESC, pay.id DESC
                                         LIMIT 1
                                     ) payment ON TRUE

                                     WHERE b.apartment_id = @ApartmentId
                                     {statusFilter}
                                     {searchFilter}

                                     ORDER BY {orderBy}
                                     OFFSET @Offset ROWS
                                     FETCH NEXT @PageSize ROWS ONLY
                                     """);

        var rows = (await connection.QueryAsync<BookingRow>(
            sql.ToString(),
            new
            {
                request.ApartmentId,
                Search = hasSearch ? $"%{EscapeLike(request.Search!.Trim())}%" : null,
                PendingStatus = (int)BookingStatus.Reserved,
                ConfirmedStatus = (int)BookingStatus.Confirmed,
                CancelledStatus = (int)BookingStatus.Cancelled,
                RejectedStatus = (int)BookingStatus.Rejected,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            })).ToList();

        if (rows.Count == 0)
        {
            return new PagedResponse<ApartmentBookingResponse>
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        var items = await ToBookingResponsesAsync(rows, cancellationToken);
        var totalCount = rows[0].TotalCount;

        return new PagedResponse<ApartmentBookingResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private async Task<IReadOnlyList<ApartmentBookingResponse>> ToBookingResponsesAsync(
        IReadOnlyList<BookingRow> rows,
        CancellationToken cancellationToken)
    {
        var tasks = rows.Select(async r =>
        {
            var guestAvatarUrl = string.IsNullOrWhiteSpace(r.GuestAvatarKey)
                ? null
                : await fileStorageService.GeneratePresignedUrlAsync(r.GuestAvatarKey, cancellationToken);

            return new ApartmentBookingResponse
            {
                Id = r.Id,
                GuestId = r.GuestId,
                GuestFullName = r.GuestFullName,
                GuestAvatarUrl = guestAvatarUrl,
                Status = r.Status,
                PaymentStatus = r.PaymentStatus,
                DurationStart = r.DurationStart,
                DurationEnd = r.DurationEnd,
                Nights = r.DurationEnd.DayNumber - r.DurationStart.DayNumber,
                TotalPriceAmount = r.TotalPriceAmount,
                TotalPriceCurrency = r.TotalPriceCurrency,
                CreatedOnUtc = r.CreatedOnUtc
            };
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    internal sealed class BookingRow
    {
        public Guid Id { get; init; }
        public Guid GuestId { get; init; }
        public string GuestFullName { get; init; } = string.Empty;
        public string? GuestAvatarKey { get; init; }
        public BookingStatus Status { get; init; }
        public PaymentStatus? PaymentStatus { get; init; }
        public DateOnly DurationStart { get; init; }
        public DateOnly DurationEnd { get; init; }
        public decimal TotalPriceAmount { get; init; }
        public string TotalPriceCurrency { get; init; } = string.Empty;
        public DateTime CreatedOnUtc { get; init; }
        public int TotalCount { get; init; }
    }
}
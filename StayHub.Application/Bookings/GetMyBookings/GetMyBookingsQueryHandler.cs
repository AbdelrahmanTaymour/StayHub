using System.Text;
using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;

namespace StayHub.Application.Bookings.GetMyBookings;

internal sealed class GetMyBookingsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    IFileStorageService fileStorageService)
    : IQueryHandler<GetMyBookingsQuery, PagedResponse<MyBookingsResponse>>
{
    public async Task<Result<PagedResponse<MyBookingsResponse>>> Handle(
        GetMyBookingsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > 50 => 50,
            _ => request.PageSize
        };

        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = new StringBuilder("""
                                    SELECT
                                        b.id AS Id,
                                        b.apartment_id AS ApartmentId,
                                        a.name AS ApartmentName,
                                        a.address_city AS ApartmentCity,
                                        img.key AS PrimaryImageKey,
                                        b.status AS Status,
                                        a.price_amount AS PricePerNight,
                                        b.total_price_amount AS TotalPriceAmount,
                                        b.total_price_currency AS TotalPriceCurrency,
                                        b.duration_start AS DurationStart,
                                        b.duration_end AS DurationEnd,
                                        COUNT(*) OVER() AS TotalCount

                                    FROM bookings b

                                    JOIN apartments a
                                        ON a.id = b.apartment_id

                                    LEFT JOIN apartment_images img
                                        ON img.apartment_id = a.id
                                        AND img.is_primary = true

                                    WHERE b.user_id = @UserId
                                    """);

        switch (request.Filter)
        {
            case MyBookingsFilter.Upcoming:
                sql.Append("""

                           AND b.status IN (@Reserved, @Confirmed)
                           AND b.duration_end >= @Today
                           """);
                break;
            case MyBookingsFilter.Completed:
                sql.Append("""

                           AND b.status = @CompletedStatus
                           """);
                break;
            case MyBookingsFilter.Cancelled:
                sql.Append("""

                           AND b.status = @CancelledStatus
                           """);
                break;
            case MyBookingsFilter.All:
            default:
                break;
        }

        sql.Append("""

                   ORDER BY b.created_on_utc DESC
                   OFFSET @Offset ROWS
                   FETCH NEXT @PageSize ROWS ONLY
                   """);

        var rows = (await connection.QueryAsync<BookingRow>(
            sql.ToString(),
            new
            {
                userContext.UserId,
                Reserved = (int)BookingStatus.Reserved,
                Confirmed = (int)BookingStatus.Confirmed,
                CompletedStatus = (int)BookingStatus.Completed,
                CancelledStatus = (int)BookingStatus.Cancelled,
                Today = DateOnly.FromDateTime(DateTime.UtcNow),
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            })).ToList();

        if (rows.Count == 0)
        {
            return new PagedResponse<MyBookingsResponse>
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        var items = await ToMyBookingsResponsesAsync(rows, cancellationToken);
        var totalCount = rows[0].TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<MyBookingsResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private async Task<IReadOnlyList<MyBookingsResponse>> ToMyBookingsResponsesAsync(
        IReadOnlyList<BookingRow> rows,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var tasks = rows.Select(async r =>
        {
            var primaryImageUrl = string.IsNullOrWhiteSpace(r.PrimaryImageKey)
                ? null
                : await fileStorageService.GeneratePresignedUrlAsync(r.PrimaryImageKey, cancellationToken);

            return new MyBookingsResponse
            {
                Id = r.Id,
                ApartmentId = r.ApartmentId,
                ApartmentName = r.ApartmentName,
                ApartmentCity = r.ApartmentCity,
                PrimaryImageUrl = primaryImageUrl,
                Status = r.Status,
                PricePerNight = r.PricePerNight,
                TotalPriceAmount = r.TotalPriceAmount,
                TotalPriceCurrency = r.TotalPriceCurrency,
                DurationStart = r.DurationStart,
                DurationEnd = r.DurationEnd,
                Nights = r.DurationEnd.DayNumber - r.DurationStart.DayNumber,
                CanCancel = r.Status is BookingStatus.Reserved or BookingStatus.Confirmed
                            && r.DurationStart > today
            };
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    internal sealed class BookingRow
    {
        public Guid Id { get; init; }
        public Guid ApartmentId { get; init; }
        public string ApartmentName { get; init; } = string.Empty;
        public string ApartmentCity { get; init; } = string.Empty;
        public string? PrimaryImageKey { get; init; }
        public BookingStatus Status { get; init; }
        public decimal PricePerNight { get; init; }
        public decimal TotalPriceAmount { get; init; }
        public string TotalPriceCurrency { get; init; } = string.Empty;
        public DateOnly DurationStart { get; init; }
        public DateOnly DurationEnd { get; init; }
        public int TotalCount { get; init; }
    }
}
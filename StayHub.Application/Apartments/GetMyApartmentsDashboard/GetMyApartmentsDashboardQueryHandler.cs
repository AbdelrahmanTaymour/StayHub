using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;

namespace StayHub.Application.Apartments.GetMyApartmentsDashboard;

internal sealed class GetMyApartmentsDashboardQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetMyApartmentsDashboardQuery, MyApartmentsDashboardResponse>
{
    private static readonly int[] OccupyingStatuses =
    [
        (int)BookingStatus.Confirmed,
        (int)BookingStatus.Completed
    ];

    public async Task<Result<MyApartmentsDashboardResponse>> Handle(
        GetMyApartmentsDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var nextMonthStart = currentMonthStart.AddMonths(1);

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               COUNT(*)::int AS Total,
                               COUNT(*) FILTER (WHERE is_active)::int AS Active
                           FROM apartments
                           WHERE owner_id = @OwnerId;

                           SELECT COUNT(*)::int
                           FROM bookings b
                           JOIN apartments a ON a.id = b.apartment_id
                           WHERE a.owner_id = @OwnerId
                             AND b.status = @Reserved
                             AND b.duration_start >= @Today;

                           SELECT
                               COALESCE(SUM(GREATEST(0,
                                   LEAST(b.duration_end, @NextMonthStart)
                                   - GREATEST(b.duration_start, @CurrentMonthStart))), 0)::int AS CurrentOccupiedNights,
                               COALESCE(SUM(GREATEST(0,
                                   LEAST(b.duration_end, @CurrentMonthStart)
                                   - GREATEST(b.duration_start, @PreviousMonthStart))), 0)::int AS PreviousOccupiedNights
                           FROM bookings b
                           JOIN apartments a ON a.id = b.apartment_id
                           WHERE a.owner_id = @OwnerId
                             AND a.is_active = true
                             AND b.status = ANY(@OccupyingStatuses)
                             AND b.duration_end > @PreviousMonthStart
                             AND b.duration_start < @NextMonthStart;

                           SELECT
                               b.total_price_currency AS Currency,
                               SUM(b.total_price_amount) AS Amount
                           FROM bookings b
                           JOIN apartments a ON a.id = b.apartment_id
                           WHERE a.owner_id = @OwnerId
                             AND a.is_active = true
                             AND b.status = ANY(@OccupyingStatuses)
                             AND b.duration_start >= @CurrentMonthStart
                             AND b.duration_start < @NextMonthStart
                           GROUP BY b.total_price_currency;
                           """;

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            request.OwnerId,
            Reserved = (int)BookingStatus.Reserved,
            OccupyingStatuses,
            Today = today,
            PreviousMonthStart = previousMonthStart,
            CurrentMonthStart = currentMonthStart,
            NextMonthStart = nextMonthStart
        });

        var counts = await multi.ReadSingleAsync<CountsRow>();
        var pending = await multi.ReadSingleAsync<int>();
        var occupancy = await multi.ReadSingleAsync<OccupancyRow>();
        var revenue = (await multi.ReadAsync<RevenueRow>()).ToList();

        return new MyApartmentsDashboardResponse
        {
            TotalCount = counts.Total,
            ActiveCount = counts.Active,
            InactiveCount = counts.Total - counts.Active,
            PendingBookingsCount = pending,
            CurrentMonth = currentMonthStart,
            CurrentMonthOccupancyRate = ToRate(
                occupancy.CurrentOccupiedNights, counts.Active, currentMonthStart),
            PreviousMonthOccupancyRate = ToRate(
                occupancy.PreviousOccupiedNights, counts.Active, previousMonthStart),
            MonthToDateRevenue = revenue
                .Select(r => new RevenueByCurrencyResponse(r.Currency, r.Amount))
                .ToList()
        };
    }

    // Occupied nights / (active apartments x days in month).
    private static double ToRate(int occupiedNights, int activeApartments, DateOnly monthStart)
    {
        if (activeApartments == 0)
        {
            return 0;
        }

        var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);

        return Math.Min(100, Math.Round(occupiedNights * 100d / (activeApartments * daysInMonth), 1));
    }

    private sealed class CountsRow
    {
        public int Total { get; init; }
        public int Active { get; init; }
    }

    private sealed class OccupancyRow
    {
        public int CurrentOccupiedNights { get; init; }
        public int PreviousOccupiedNights { get; init; }
    }

    private sealed class RevenueRow
    {
        public string Currency { get; init; } = string.Empty;
        public decimal Amount { get; init; }
    }
}
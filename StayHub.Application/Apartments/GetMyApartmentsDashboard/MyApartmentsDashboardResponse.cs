namespace StayHub.Application.Apartments.GetMyApartmentsDashboard;

public sealed record MyApartmentsDashboardResponse
{
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int InactiveCount { get; init; }
    public int PendingBookingsCount { get; init; }

    public DateOnly CurrentMonth { get; init; }

    public double CurrentMonthOccupancyRate { get; init; } // 0-100
    public double PreviousMonthOccupancyRate { get; init; } // 0-100

    // Grouped by currency: summing across currencies is meaningless.
    public IReadOnlyList<RevenueByCurrencyResponse> MonthToDateRevenue { get; init; }
}

public sealed record RevenueByCurrencyResponse(string Currency, decimal Amount);
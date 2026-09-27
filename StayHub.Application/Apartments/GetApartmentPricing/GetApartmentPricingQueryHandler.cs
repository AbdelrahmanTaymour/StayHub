using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Shared;

namespace StayHub.Application.Apartments.GetApartmentPricing;

internal sealed class GetApartmentPricingQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    PricingService pricingService)
    : IQueryHandler<GetApartmentPricingQuery, ApartmentPricingResponse>
{
    // Mirrors SearchApartmentsQueryHandler: only a Confirmed booking blocks a date range.
    private static readonly int[] ActiveBookingStatuses = [(int)BookingStatus.Confirmed];

    public async Task<Result<ApartmentPricingResponse>> Handle(
        GetApartmentPricingQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Start >= request.End)
        {
            return Result.Failure<ApartmentPricingResponse>(ApartmentErrors.InvalidDateRange);
        }

        using var connection = sqlConnectionFactory.CreateConnection();

        const string apartmentSql = """
                                    SELECT
                                        a.price_amount AS PriceAmount,
                                        a.price_currency AS Currency,
                                        a.cleaning_fee_amount AS CleaningFeeAmount,
                                        a.amenities AS Amenities,
                                        a.is_active AS IsActive
                                    FROM apartments a
                                    WHERE a.id = @ApartmentId
                                    """;

        var apartment = await connection.QueryFirstOrDefaultAsync<ApartmentPricingRow>(
            apartmentSql, new { request.ApartmentId });

        if (apartment is null || !apartment.IsActive)
        {
            return Result.Failure<ApartmentPricingResponse>(ApartmentErrors.NotFound);
        }

        const string availabilitySql = """
                                       SELECT NOT EXISTS (
                                           SELECT 1
                                           FROM bookings b
                                           WHERE b.apartment_id = @ApartmentId
                                             AND b.status = ANY(@ActiveBookingStatuses)
                                             AND b.duration_start < @End
                                             AND b.duration_end > @Start
                                       )
                                       AND NOT EXISTS (
                                           SELECT 1
                                           FROM apartment_availability_blocks ab
                                           WHERE ab.apartment_id = @ApartmentId
                                             AND ab.start < @End
                                             AND ab."end" > @Start
                                       )
                                       """;

        var isAvailable = await connection.ExecuteScalarAsync<bool>(
            availabilitySql,
            new
            {
                request.ApartmentId,
                ActiveBookingStatuses,
                request.Start,
                request.End
            });

        if (!isAvailable)
        {
            return new ApartmentPricingResponse
            {
                IsAvailable = false,
                Currency = apartment.Currency
            };
        }

        var nights = request.End.DayNumber - request.Start.DayNumber;

        var amenities = (apartment.Amenities ?? [])
            .Select(Enum.Parse<Amenity>)
            .ToList();

        var pricing = pricingService.CalculatePrice(
            new Money(apartment.PriceAmount, Currency.FromCode(apartment.Currency)),
            new Money(apartment.CleaningFeeAmount, Currency.FromCode(apartment.Currency)),
            amenities,
            nights);

        return new ApartmentPricingResponse
        {
            IsAvailable = true,
            Nights = nights,
            PricePerNight = apartment.PriceAmount,
            SubtotalForStay = pricing.PriceForPeriod.Amount,
            CleaningFee = pricing.CleaningFee.Amount,
            AmenitiesUpcharge = pricing.AmenitiesUpCharge.Amount,
            TotalPrice = pricing.TotalPrice.Amount,
            Currency = apartment.Currency
        };
    }

    private sealed class ApartmentPricingRow
    {
        public decimal PriceAmount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public decimal CleaningFeeAmount { get; init; }
        public IReadOnlyList<string>? Amenities { get; init; }
        public bool IsActive { get; init; }
    }
}
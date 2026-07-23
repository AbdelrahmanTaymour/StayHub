using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.GetApartment;

internal sealed class GetApartmentQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory) : IQueryHandler<GetApartmentQuery, ApartmentResponse>
{
    public async Task<Result<ApartmentResponse>> Handle(
        GetApartmentQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT 
                               a.id AS Id,
                               a.owner_id AS OwnerId,
                               a.name AS Name,
                               a.description AS Description,
                               a.price_amount AS PriceAmount,
                               a.price_currency AS PriceCurrency,
                               a.cleaning_fee_amount AS CleaningFeeAmount,
                               a.cleaning_fee_currency AS CleaningFeeCurrency,
                               a.is_active AS IsActive,
                               a.address_country AS Country,
                               a.address_state AS State,
                               a.address_zip_code AS ZipCode,
                               a.address_city AS City,
                               a.address_street AS Street
                           FROM apartments AS a
                           WHERE a.id = @ApartmentId;

                           SELECT 
                               aa.amenity AS Amenity
                           FROM apartment_amenities AS aa
                           WHERE aa.apartment_id = @ApartmentId;

                           SELECT 
                               ai.id AS Id,
                               ai.url AS Url,
                               ai.display_order AS DisplayOrder,
                               ai.is_primary AS IsPrimary
                           FROM apartment_images AS ai
                           WHERE ai.apartment_id = @ApartmentId
                           ORDER BY ai.display_order ASC;
                           """;

        using var multi = await connection.QueryMultipleAsync(
            sql,
            new
            {
                request.ApartmentId
            });

        // 1. Read a single apartment row directly
        var apartment = multi.Read<ApartmentResponse, AddressResponse, ApartmentResponse>(
            (apt, address) =>
            {
                apt.Address = address;
                return apt;
            },
            "Country").FirstOrDefault();

        if (apartment is null) return Result.Failure<ApartmentResponse>(ApartmentErrors.NotFound);

        // 2. Read collections efficiently
        var amenities = await multi.ReadAsync<Amenity>();
        var images = await multi.ReadAsync<ApartmentImageResponse>();

        apartment.Amenities = amenities.ToList();
        apartment.Images = images.ToList();

        return apartment;
    }
}
using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.GetApartmentForEdit;

internal sealed class GetApartmentForEditQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetApartmentForEditQuery, ApartmentForEditResponse>
{
    public async Task<Result<ApartmentForEditResponse>> Handle(
        GetApartmentForEditQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               a.id AS Id,
                               a.owner_id AS OwnerId,
                               a.name AS Title,
                               a.description AS Description,
                               a.is_active AS IsActive,
                               a.price_amount AS NightlyRate,
                               a.price_currency AS Currency,
                               a.cleaning_fee_amount AS CleaningFee,
                               a.address_street AS Street,
                               a.address_city AS City,
                               a.address_zip_code AS ZipCode,
                               a.address_country AS Country,
                               a.address_state AS State
                           FROM apartments AS a
                           WHERE a.id = @ApartmentId
                             AND (a.owner_id = @UserId OR @IsAdmin);
                           """;

        var row = await connection.QuerySingleOrDefaultAsync<ApartmentEditRow>(
            sql,
            new
            {
                request.ApartmentId,
                userContext.UserId,
                userContext.IsAdmin
            });

        if (row is null)
        {
            return Result.Failure<ApartmentForEditResponse>(ApartmentErrors.NotFound);
        }

        if (!userContext.IsOwner(row.OwnerId) && !userContext.IsAdmin)
        {
            return Result.Failure<ApartmentForEditResponse>(ApartmentErrors.NotAuthorized);
        }

        return new ApartmentForEditResponse
        {
            Id = row.Id,
            Title = row.Title,
            Description = row.Description,
            IsActive = row.IsActive,
            Pricing = new ApartmentPricingResponse
            {
                Currency = row.Currency,
                NightlyRate = row.NightlyRate,
                CleaningFee = row.CleaningFee
            },
            Address = new ApartmentAddressResponse
            {
                Street = row.Street,
                City = row.City,
                ZipCode = row.ZipCode,
                Country = row.Country,
                State = row.State
            }
        };
    }

    private sealed class ApartmentEditRow
    {
        public Guid Id { get; init; }
        public Guid OwnerId { get; init; }

        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }

        public bool IsActive { get; init; }

        public decimal NightlyRate { get; init; }
        public string Currency { get; init; } = string.Empty;
        public decimal CleaningFee { get; init; }

        public string Street { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string ZipCode { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
    }
}
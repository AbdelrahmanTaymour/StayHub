using System.Data;
using Bogus;
using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Domain.Apartments;

namespace StayHub.Api.Extensions;

public static class SeedDataExtensions
{
    public static void SeedData(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        var sqlConnectionFactory = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();
        using var connection = sqlConnectionFactory.CreateConnection();

        const string checkSql = "SELECT EXISTS(SELECT 1 FROM public.users LIMIT 1);";
        var hasData = connection.ExecuteScalar<bool>(checkSql);
        if (hasData) return;

        Randomizer.Seed = new Random(8675309);
        var faker = new Faker();
        var ownerId = Guid.CreateVersion7();

        var owner = new
        {
            Id = ownerId,
            FirstName = faker.Name.FirstName(),
            LastName = faker.Name.LastName(),
            Email = faker.Internet.Email(),
            CreatedOnUtc = DateTime.UtcNow
        };

        var apartments = new List<object>();
        for (var i = 0; i < 100; i++)
            apartments.Add(new
            {
                Id = Guid.CreateVersion7(),
                OwnerId = ownerId,
                Name = faker.Company.CompanyName(),
                Description = "Amazing view",
                Country = faker.Address.Country(),
                State = faker.Address.State(),
                ZipCode = faker.Address.ZipCode(),
                City = faker.Address.City(),
                Street = faker.Address.StreetAddress(),
                PriceAmount = faker.Random.Decimal(50, 1000),
                PriceCurrency = "USD",
                CleaningFeeAmount = faker.Random.Decimal(25, 200),
                CleaningFeeCurrency = "USD",
                IsActive = true,
                CreatedOnUtc = DateTime.UtcNow,
                Amenities = new[] { (int)Amenity.Parking, (int)Amenity.MountainView },
                LastBookedOn = (DateTime?)null
            });

        const string ownerSql = """
                                INSERT INTO public.users
                                (id, first_name, last_name, email, created_on_utc)
                                VALUES (@Id, @FirstName, @LastName, @Email, @CreatedOnUtc);
                                """;

        const string apartmentSql = """
                                    INSERT INTO public.apartments
                                    (
                                        id, owner_id, name, description, 
                                        address_country, address_state, address_zip_code, address_city, address_street, 
                                        price_amount, price_currency, cleaning_fee_amount, cleaning_fee_currency, 
                                        is_active, created_on_utc, amenities, last_booked_on_utc
                                    )
                                    VALUES 
                                    (
                                        @Id, @OwnerId, @Name, @Description, 
                                        @Country, @State, @ZipCode, @City, @Street, 
                                        @PriceAmount, @PriceCurrency, @CleaningFeeAmount, @CleaningFeeCurrency, 
                                        @IsActive, @CreatedOnUtc, @Amenities, @LastBookedOn
                                    );
                                    """;

        if (connection.State != ConnectionState.Open) connection.Open();

        using var transaction = connection.BeginTransaction();

        connection.Execute(ownerSql, owner, transaction);
        connection.Execute(apartmentSql, apartments, transaction);

        transaction.Commit();
    }
}
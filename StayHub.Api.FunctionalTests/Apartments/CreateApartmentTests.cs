using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class CreateApartmentTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Create_ShouldReturnCreatedWithLocationAndId_WhenRequestIsValid()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest();

        // Act
        var response = await HttpClient.PostAsJsonAsync(ApartmentRoutes.Search(), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var id = await response.Content.ReadFromJsonAsync<Guid>();

        id.Should().NotBe(Guid.Empty);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenCallerIsNotAuthenticated()
    {
        // Arrange
        var request = ApartmentTestData.ValidCreateRequest();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("")]
    public async Task Create_ShouldReturnValidationProblem_WhenNameIsEmpty(string name)
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(name: name);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Name");
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenNameExceedsMaxLength()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(
            name: new string('a', 201));

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenDescriptionIsEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(
            description: "");

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenDescriptionExceedsMaxLength()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(
            description: new string('a', 2001));

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    public async Task Create_ShouldReturnValidationProblem_WhenStreetIsEmpty(string street)
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(street: street);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenCityIsEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(city: "");

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenCountryIsEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(country: "");

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenStateIsEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(state: "");

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenZipCodeIsEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(zipCode: "");

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenPriceAmountIsZero()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(priceAmount: 0);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenPriceAmountIsNegative()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(priceAmount: -10);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenCleaningFeeIsNegative()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(
            cleaningFeeAmount: -5);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenCleaningFeeIsZero()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(
            cleaningFeeAmount: 0);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenPriceCurrencyIsUnsupported()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(
            priceCurrency: "ZZZ");

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenCleaningFeeCurrencyIsUnsupported()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(
            cleaningFeeCurrency: "ZZZ");

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenPriceCurrencyIsEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest(
            priceCurrency: "");

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldAssignCallerAsOwner_WhenApartmentIsCreated()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest();

        // Act
        var createResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        createResponse.EnsureSuccessStatusCode();

        var apartmentId =
            await createResponse.Content.ReadFromJsonAsync<Guid>();

        var getResponse = await HttpClient.GetAsync(
            ApartmentRoutes.ById(apartmentId));

        getResponse.EnsureSuccessStatusCode();

        var body =
            await getResponse.Content.ReadFromJsonAsync<JsonElement>();

        var ownerId = body.GetProperty("ownerId").GetGuid();

        // Assert
        ownerId.Should().Be(userId);
    }
}
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class AddApartmentImageTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task AddImage_ShouldReturnCreated_WhenOwnerUploadsValidJpeg()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsync(
            ApartmentRoutes.Images(apartmentId), ApartmentTestData.BuildImageContent());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var imageId = await response.Content.ReadFromJsonAsync<Guid>();
        imageId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task AddImage_ShouldReturnForbidden_WhenUserIsNotApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsync(
            ApartmentRoutes.Images(apartmentId), ApartmentTestData.BuildImageContent());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddImage_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsync(
            ApartmentRoutes.Images(Guid.NewGuid()), ApartmentTestData.BuildImageContent());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddImage_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange

        // Act
        var response = await HttpClient.PostAsync(
            ApartmentRoutes.Images(Guid.NewGuid()), ApartmentTestData.BuildImageContent());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddImage_ShouldReturnBadRequest_WhenFileExtensionIsNotAllowed()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var content = ApartmentTestData.BuildImageContent(fileName: "document.pdf", contentType: "application/pdf");

        // Act
        var response = await HttpClient.PostAsync(
            ApartmentRoutes.Images(apartmentId), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddImage_ShouldReturnBadRequest_WhenFileContentTypeIsNotAllowed()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        // Extension is allowed, but the declared content type is not.
        var content =
            ApartmentTestData.BuildImageContent(fileName: "photo.jpg", contentType: "application/octet-stream");

        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Images(apartmentId), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddImage_ShouldReturnBadRequest_WhenFileIsEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var content = ApartmentTestData.BuildImageContent(bytes: []);

        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Images(apartmentId), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddImage_ShouldReturnBadRequest_WhenFileExceedsMaximumSize()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var oversizedFile = new byte[5 * 1024 * 1024 + 1];
        var content = ApartmentTestData.BuildImageContent(bytes: oversizedFile);

        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Images(apartmentId), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddImage_ShouldReturnCreated_WhenOwnerUploadsValidPng()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var content = ApartmentTestData.BuildImageContent(fileName: "photo.png", contentType: "image/png");

        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Images(apartmentId), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
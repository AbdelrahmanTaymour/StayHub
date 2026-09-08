using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class ImageManagementTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    // ---- Remove ----

    [Fact]
    public async Task RemoveImage_ShouldReturnNoContent_WhenCallerIsApartmentOwner()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);
        var imageId = await ApartmentTestData.AddImageAsync(HttpClient, apartmentId);

        // Act
        var response = await HttpClient.DeleteAsync(
            ApartmentRoutes.ImageById(imageId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveImage_ShouldReturnForbidden_WhenCallerIsNotApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);
        var imageId = await ApartmentTestData.AddImageAsync(HttpClient, apartmentId);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.DeleteAsync(
            ApartmentRoutes.ImageById(imageId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveImage_ShouldReturnNotFound_WhenImageDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.DeleteAsync(
            ApartmentRoutes.ImageById(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- Reorder ----

    [Fact]
    public async Task ReorderImages_ShouldReturnNoContent_WhenCallerIsOwnerAndOrderIsValid()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var firstImageId = await ApartmentTestData.AddImageAsync(HttpClient, apartmentId);
        var secondImageId = await ApartmentTestData.AddImageAsync(HttpClient, apartmentId);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ImagesOrder(apartmentId),
            new
            {
                OrderedImageIds = new[]
                {
                    secondImageId,
                    firstImageId
                }
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReorderImages_ShouldReturnForbidden_WhenCallerIsNotApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);
        var imageId = await ApartmentTestData.AddImageAsync(HttpClient, apartmentId);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ImagesOrder(apartmentId),
            new
            {
                OrderedImageIds = new[]
                {
                    imageId
                }
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReorderImages_ShouldReturnBadRequest_WhenOrderedImageIdsDoNotMatchApartmentImages()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);
        await ApartmentTestData.AddImageAsync(HttpClient, apartmentId);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ImagesOrder(apartmentId),
            new
            {
                OrderedImageIds = new[]
                {
                    Guid.NewGuid()
                }
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderImages_ShouldReturnBadRequest_WhenOrderedImageIdsAreEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ImagesOrder(apartmentId),
            new
            {
                OrderedImageIds = Array.Empty<Guid>()
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderImages_ShouldReturnBadRequest_WhenOrderedImageIdsContainDuplicates()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);
        var imageId = await ApartmentTestData.AddImageAsync(HttpClient, apartmentId);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ImagesOrder(apartmentId),
            new
            {
                OrderedImageIds = new[]
                {
                    imageId,
                    imageId
                }
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderImages_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ImagesOrder(Guid.NewGuid()),
            new
            {
                OrderedImageIds = new[]
                {
                    Guid.NewGuid()
                }
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
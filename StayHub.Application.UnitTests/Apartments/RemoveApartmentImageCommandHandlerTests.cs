using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Apartments.RemoveApartmentImage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.UnitTests.Apartments;

public class RemoveApartmentImageCommandHandlerTests
{
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();

    private readonly RemoveApartmentImageCommandHandler _handler;
    private readonly IApartmentImageRepository _imageRepositoryMock = Substitute.For<IApartmentImageRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public RemoveApartmentImageCommandHandlerTests()
    {
        _handler = new RemoveApartmentImageCommandHandler(
            _imageRepositoryMock,
            _apartmentRepositoryMock,
            _userContextMock,
            _unitOfWorkMock);
    }

    private static ApartmentImage CreateImage(Guid apartmentId) =>
        ApartmentImage.Create(apartmentId, new ImageUrl("https://cdn.stayhub.dev/a.png"), 0, DateTime.UtcNow);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenImageNotFound()
    {
        // Arrange
        var imageId = Guid.CreateVersion7();
        _imageRepositoryMock.GetByIdAsync(imageId, Arg.Any<CancellationToken>()).Returns((ApartmentImage?)null);

        // Act
        var result = await _handler.Handle(new RemoveApartmentImageCommand(imageId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentImageErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange — an orphaned image referencing a deleted apartment.
        var image = CreateImage(Guid.CreateVersion7());
        _imageRepositoryMock.GetByIdAsync(image.Id, Arg.Any<CancellationToken>()).Returns(image);
        _apartmentRepositoryMock.GetByIdAsync(image.ApartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(new RemoveApartmentImageCommand(image.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var image = CreateImage(apartment.Id);
        _imageRepositoryMock.GetByIdAsync(image.Id, Arg.Any<CancellationToken>()).Returns(image);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(new RemoveApartmentImageCommand(image.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotAuthorized);
        _imageRepositoryMock.DidNotReceive().Remove(Arg.Any<ApartmentImage>());
    }

    [Fact]
    public async Task Handle_Should_RemoveImageAndSaveChanges_WhenCallerOwnsApartment()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var image = CreateImage(apartment.Id);
        _imageRepositoryMock.GetByIdAsync(image.Id, Arg.Any<CancellationToken>()).Returns(image);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new RemoveApartmentImageCommand(image.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _imageRepositoryMock.Received(1).Remove(image);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Apartments.SetAsPrimaryImage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.UnitTests.Apartments;

public class SetAsPrimaryImageCommandHandlerTests
{
    private readonly IApartmentRepository _apartmentRepositoryMock =
        Substitute.For<IApartmentRepository>();

    private readonly SetAsPrimaryImageCommandHandler _handler;

    private readonly IApartmentImageRepository _imageRepositoryMock =
        Substitute.For<IApartmentImageRepository>();

    private readonly IUnitOfWork _unitOfWorkMock =
        Substitute.For<IUnitOfWork>();

    private readonly IUserContext _userContextMock =
        Substitute.For<IUserContext>();

    public SetAsPrimaryImageCommandHandlerTests()
    {
        _handler = new SetAsPrimaryImageCommandHandler(
            _apartmentRepositoryMock,
            _imageRepositoryMock,
            _userContextMock,
            _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var imageId = Guid.CreateVersion7();

        _apartmentRepositoryMock
            .GetByIdAsync(apartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(
            new SetAsPrimaryImageCommand(apartmentId, imageId),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);

        await _imageRepositoryMock.DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await _unitOfWorkMock.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var imageId = Guid.CreateVersion7();

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock
            .IsOwner(apartment.OwnerId)
            .Returns(false);

        _userContextMock
            .IsAdmin
            .Returns(false);

        // Act
        var result = await _handler.Handle(
            new SetAsPrimaryImageCommand(apartment.Id, imageId),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotAuthorized);

        await _imageRepositoryMock.DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await _unitOfWorkMock.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenImageNotFound()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock
            .IsOwner(apartment.OwnerId)
            .Returns(true);

        var imageId = Guid.CreateVersion7();

        _imageRepositoryMock
            .GetByIdAsync(imageId, Arg.Any<CancellationToken>())
            .Returns((ApartmentImage?)null);

        // Act
        var result = await _handler.Handle(
            new SetAsPrimaryImageCommand(apartment.Id, imageId),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentImageErrors.NotFound);

        await _unitOfWorkMock.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenImageBelongsToAnotherApartment()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        var image = ApartmentData.CreateImage(
            apartmentId: Guid.CreateVersion7());

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock
            .IsOwner(apartment.OwnerId)
            .Returns(true);

        _imageRepositoryMock
            .GetByIdAsync(image.Id, Arg.Any<CancellationToken>())
            .Returns(image);

        // Act
        var result = await _handler.Handle(
            new SetAsPrimaryImageCommand(apartment.Id, image.Id),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentImageErrors.NotFound);

        await _imageRepositoryMock.DidNotReceive()
            .GetPrimaryByApartmentIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await _unitOfWorkMock.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldUnsetCurrentPrimaryAndSetNewImageAsPrimary()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        var currentPrimaryImage = ApartmentData.CreateImage(
            apartment.Id,
            isPrimary: true);

        var newPrimaryImage = ApartmentData.CreateImage(
            apartment.Id,
            isPrimary: false);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock
            .IsOwner(apartment.OwnerId)
            .Returns(true);

        _imageRepositoryMock
            .GetByIdAsync(
                newPrimaryImage.Id,
                Arg.Any<CancellationToken>())
            .Returns(newPrimaryImage);

        _imageRepositoryMock
            .GetPrimaryByApartmentIdAsync(
                apartment.Id,
                Arg.Any<CancellationToken>())
            .Returns(currentPrimaryImage);

        // Act
        var result = await _handler.Handle(
            new SetAsPrimaryImageCommand(
                apartment.Id,
                newPrimaryImage.Id),
            default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        currentPrimaryImage.IsPrimary.Should().BeFalse();
        newPrimaryImage.IsPrimary.Should().BeTrue();

        await _unitOfWorkMock.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSetImageAsPrimary_WhenNoCurrentPrimaryExists()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        var image = ApartmentData.CreateImage(
            apartment.Id,
            isPrimary: false);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock
            .IsOwner(apartment.OwnerId)
            .Returns(true);

        _imageRepositoryMock
            .GetByIdAsync(
                image.Id,
                Arg.Any<CancellationToken>())
            .Returns(image);

        _imageRepositoryMock
            .GetPrimaryByApartmentIdAsync(
                apartment.Id,
                Arg.Any<CancellationToken>())
            .Returns((ApartmentImage?)null);

        // Act
        var result = await _handler.Handle(
            new SetAsPrimaryImageCommand(apartment.Id, image.Id),
            default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        image.IsPrimary.Should().BeTrue();

        await _unitOfWorkMock.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotChangeImages_WhenImageIsAlreadyPrimary()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        var image = ApartmentData.CreateImage(
            apartment.Id,
            isPrimary: true);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock
            .IsOwner(apartment.OwnerId)
            .Returns(true);

        _imageRepositoryMock
            .GetByIdAsync(
                image.Id,
                Arg.Any<CancellationToken>())
            .Returns(image);

        _imageRepositoryMock
            .GetPrimaryByApartmentIdAsync(
                apartment.Id,
                Arg.Any<CancellationToken>())
            .Returns(image);

        // Act
        var result = await _handler.Handle(
            new SetAsPrimaryImageCommand(apartment.Id, image.Id),
            default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        image.IsPrimary.Should().BeTrue();

        await _unitOfWorkMock.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
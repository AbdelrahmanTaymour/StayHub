using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Apartments.ReorderApartmentImages;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Apartments;

public class ReorderApartmentImagesCommandHandlerTests
{
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();

    private readonly ReorderApartmentImagesCommandHandler _handler;
    private readonly IApartmentImageRepository _imageRepositoryMock = Substitute.For<IApartmentImageRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public ReorderApartmentImagesCommandHandlerTests()
    {
        _handler = new ReorderApartmentImagesCommandHandler(
            _apartmentRepositoryMock,
            _imageRepositoryMock,
            _userContextMock,
            _unitOfWorkMock);
    }

    private static ApartmentImage CreateImage(Guid apartmentId, int displayOrder) =>
        ApartmentImage.Create(apartmentId, new ImageUrl($"https://cdn.stayhub.dev/{displayOrder}.png"), displayOrder,
            DateTime.UtcNow);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        _apartmentRepositoryMock.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(new ReorderApartmentImagesCommand(apartmentId, []), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(new ReorderApartmentImagesCommand(apartment.Id, []), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotAuthorized);
        await _imageRepositoryMock.DidNotReceive().GetByApartmentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenCallerIsAdminActingOnSomeoneElsesApartment()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var image = CreateImage(apartment.Id, 0);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _imageRepositoryMock.GetByApartmentIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns([image]);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([Role.Admin.Name]);

        // Act
        var result = await _handler.Handle(new ReorderApartmentImagesCommand(apartment.Id, [image.Id]), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenOrderedIdsCountDoesNotMatchExistingImages()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var image = CreateImage(apartment.Id, 0);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _imageRepositoryMock.GetByApartmentIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns([image]);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act — submitting two ids when only one image exists
        var result = await _handler.Handle(
            new ReorderApartmentImagesCommand(apartment.Id, [image.Id, Guid.CreateVersion7()]),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentImageErrors.InvalidOrderPayload);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenOrderedIdsDoNotMatchExistingImageIds()
    {
        // Arrange — same count, but a completely different (unknown) id.
        var apartment = ApartmentData.Create();
        var image = CreateImage(apartment.Id, 0);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _imageRepositoryMock.GetByApartmentIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns([image]);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(
            new ReorderApartmentImagesCommand(apartment.Id, [Guid.CreateVersion7()]),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentImageErrors.InvalidOrderPayload);
    }

    [Fact]
    public async Task Handle_Should_ReorderImagesToMatchRequestedSequence_AndSaveChanges()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var first = CreateImage(apartment.Id, 0);
        var second = CreateImage(apartment.Id, 1);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _imageRepositoryMock.GetByApartmentIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns([first, second]);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act — reversed order: second should end up first
        var result = await _handler.Handle(
            new ReorderApartmentImagesCommand(apartment.Id, [second.Id, first.Id]),
            default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        second.DisplayOrder.Should().Be(0);
        first.DisplayOrder.Should().Be(1);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
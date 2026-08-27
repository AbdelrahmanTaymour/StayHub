using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Favorites.AddFavoriteApartment;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Favorites;

namespace StayHub.Application.UnitTests.Favorites;

public class AddFavoriteApartmentTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;

    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly IFavoriteApartmentRepository _favoriteApartmentRepositoryMock =
        Substitute.For<IFavoriteApartmentRepository>();

    private readonly AddFavoriteApartmentCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public AddFavoriteApartmentTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new AddFavoriteApartmentCommandHandler(
            _apartmentRepositoryMock,
            _favoriteApartmentRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        _apartmentRepositoryMock.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(new AddFavoriteApartmentCommand(apartmentId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenAlreadyFavorited()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var userId = Guid.CreateVersion7();
        var existing = FavoriteApartment.Create(userId, apartment.Id, UtcNow);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(userId);
        _favoriteApartmentRepositoryMock.GetAsync(userId, apartment.Id, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _handler.Handle(new AddFavoriteApartmentCommand(apartment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FavoriteApartmentErrors.AlreadyFavorited);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_AddFavoriteAndSaveChanges_WhenValid()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var userId = Guid.CreateVersion7();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(userId);
        _favoriteApartmentRepositoryMock
            .GetAsync(userId, apartment.Id, Arg.Any<CancellationToken>())
            .Returns((FavoriteApartment?)null);

        // Act
        var result = await _handler.Handle(new AddFavoriteApartmentCommand(apartment.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _favoriteApartmentRepositoryMock.Received(1).Add(Arg.Is<FavoriteApartment>(f =>
            f.UserId == userId && f.ApartmentId == apartment.Id));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
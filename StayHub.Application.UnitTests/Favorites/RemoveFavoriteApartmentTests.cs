using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Favorites.RemoveFavoriteApartment;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Favorites;

namespace StayHub.Application.UnitTests.Favorites;

public class RemoveFavoriteApartmentTests
{
    private readonly IFavoriteApartmentRepository _favoriteApartmentRepositoryMock =
        Substitute.For<IFavoriteApartmentRepository>();

    private readonly RemoveFavoriteApartmentCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public RemoveFavoriteApartmentTests()
    {
        _handler = new RemoveFavoriteApartmentCommandHandler(
            _favoriteApartmentRepositoryMock,
            _userContextMock,
            _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenFavoriteNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(userId);
        _favoriteApartmentRepositoryMock
            .GetAsync(userId, apartmentId, Arg.Any<CancellationToken>())
            .Returns((FavoriteApartment?)null);

        // Act
        var result = await _handler.Handle(new RemoveFavoriteApartmentCommand(apartmentId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FavoriteApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_RemoveFavoriteAndSaveChanges_WhenValid()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var favorite = FavoriteApartment.Create(userId, apartmentId, DateTime.UtcNow);
        _userContextMock.UserId.Returns(userId);
        _favoriteApartmentRepositoryMock.GetAsync(userId, apartmentId, Arg.Any<CancellationToken>()).Returns(favorite);

        // Act
        var result = await _handler.Handle(new RemoveFavoriteApartmentCommand(apartmentId), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _favoriteApartmentRepositoryMock.Received(1).Remove(favorite);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
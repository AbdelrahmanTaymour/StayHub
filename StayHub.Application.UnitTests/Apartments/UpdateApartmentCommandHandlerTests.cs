using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Apartments.UpdateApartment;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Apartments;

public class UpdateApartmentCommandHandlerTests
{
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();

    private readonly UpdateApartmentCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public UpdateApartmentCommandHandlerTests()
    {
        _handler = new UpdateApartmentCommandHandler(_apartmentRepositoryMock, _userContextMock, _unitOfWorkMock);
    }

    private static UpdateApartmentCommand CommandFor(Guid apartmentId) => new(
        ApartmentId: apartmentId,
        Name: "Updated name",
        Description: "Updated description",
        PriceAmount: 150m,
        PriceCurrency: "USD",
        CleaningFeeAmount: 25m,
        CleaningFeeCurrency: "USD");

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        _apartmentRepositoryMock.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(CommandFor(apartmentId), default);

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
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotAuthorized);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_UpdateDetailsAndSaveChanges_WhenCallerIsOwner()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        apartment.Name.Should().Be(new Name("Updated name"));
        apartment.Description.Should().Be(new Description("Updated description"));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_UpdateDetails_WhenCallerIsAdminActingOnSomeoneElsesApartment()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([Role.Admin.Name]);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        apartment.Name.Should().Be(new Name("Updated name"));
    }

    [Fact]
    public async Task Handle_Should_Throw_WhenPriceCurrencyIsUnrecognized()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        var command = CommandFor(apartment.Id) with { PriceCurrency = "XYZ" };

        // Act
        var act = () => _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ApplicationException>();
    }
}
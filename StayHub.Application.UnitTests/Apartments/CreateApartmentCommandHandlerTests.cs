using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Apartments.CreateApartment;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.UnitTests.Apartments;

public class CreateApartmentCommandHandlerTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;

    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly CreateApartmentCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public CreateApartmentCommandHandlerTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new CreateApartmentCommandHandler(
            _apartmentRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static CreateApartmentCommand ValidCommand() => new(
        Name: "Test apartment",
        Description: "Test description",
        Street: "Street",
        City: "City",
        State: "State",
        ZipCode: "ZipCode",
        Country: "Country",
        PriceAmount: 100m,
        PriceCurrency: "USD",
        CleaningFeeAmount: 20m,
        CleaningFeeCurrency: "USD");

    [Fact]
    public async Task Handle_Should_SetOwnerIdFromUserContext_NotFromRequest()
    {
        // Arrange — CreateApartmentCommand has no OwnerId field at all;
        // "who owns this" is resolved server-side, never client-supplied.
        var ownerId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(ownerId);
        Apartment? added = null;
        _apartmentRepositoryMock.When(r => r.Add(Arg.Any<Apartment>())).Do(call => added = call.Arg<Apartment>());

        // Act
        await _handler.Handle(ValidCommand(), default);

        // Assert
        added.Should().NotBeNull();
        added!.OwnerId.Should().Be(ownerId);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessWithApartmentId_AndSaveChanges()
    {
        // Arrange
        _userContextMock.UserId.Returns(Guid.CreateVersion7());

        // Act
        var result = await _handler.Handle(ValidCommand(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _apartmentRepositoryMock.Received(1).Add(Arg.Is<Apartment>(a => a.Id == result.Value));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task Handle_Should_Throw_WhenPriceCurrencyIsUnrecognized()
    {
        // Arrange
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        var command = ValidCommand() with { PriceCurrency = "XYZ" };

        // Act
        var act = () => _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ApplicationException>();
    }
}
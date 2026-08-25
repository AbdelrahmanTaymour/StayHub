using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Bookings.GetBookingsByUser;
using StayHub.Domain.Bookings;

namespace StayHub.Application.UnitTests.Bookings;

public class GetBookingsByUserTests
{
    private readonly GetBookingsByUserQueryHandler _handler;
    private readonly ISqlConnectionFactory _sqlConnectionFactoryMock = Substitute.For<ISqlConnectionFactory>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public GetBookingsByUserTests()
    {
        _handler = new GetBookingsByUserQueryHandler(_sqlConnectionFactoryMock, _userContextMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotSelfOrAdmin()
    {
        // Arrange
        var targetUserId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(new GetBookingsByUserQuery(targetUserId, Page: 1, PageSize: 20), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_NotOpenDatabaseConnection_WhenCallerIsNotSelfOrAdmin()
    {
        // Arrange
        var targetUserId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        await _handler.Handle(new GetBookingsByUserQuery(targetUserId, Page: 1, PageSize: 20), default);

        // Assert
        _sqlConnectionFactoryMock.DidNotReceive().CreateConnection();
    }
}
using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Apartments.SearchApartments;
using StayHub.Domain.Bookings;

namespace StayHub.Application.UnitTests.Apartments;

public class SearchApartmentsQueryHandlerTests
{
    private readonly SearchApartmentsQueryHandler _handler;
    private readonly PricingService _pricingService = new();
    private readonly ISqlConnectionFactory _sqlConnectionFactoryMock = Substitute.For<ISqlConnectionFactory>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public SearchApartmentsQueryHandlerTests()
    {
        _handler = new SearchApartmentsQueryHandler(
            _sqlConnectionFactoryMock,
            _userContextMock,
            _pricingService);
    }

    [Theory]
    [InlineData(2026, 1, 10, 2026, 1, 1)]
    [InlineData(2026, 1, 1, 2026, 1, 1)]
    public async Task Handle_Should_ReturnEmptyPage_WhenStartIsNotBeforeEnd(
        int startYear, int startMonth, int startDay,
        int endYear, int endMonth, int endDay)
    {
        // Arrange
        var query = new SearchApartmentsQuery(
            City: null,
            MinPrice: null,
            MaxPrice: null,
            Start: new DateOnly(startYear, startMonth, startDay),
            End: new DateOnly(endYear, endMonth, endDay),
            Page: 1,
            PageSize: 20);

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_Should_NotOpenDatabaseConnection_WhenStartIsNotBeforeEnd()
    {
        // Arrange
        var query = new SearchApartmentsQuery(
            City: null,
            MinPrice: null,
            MaxPrice: null,
            Start: new DateOnly(2026, 1, 10),
            End: new DateOnly(2026, 1, 1),
            Page: 1,
            PageSize: 20);

        // Act
        await _handler.Handle(query, default);

        // Assert
        _sqlConnectionFactoryMock.DidNotReceive().CreateConnection();
    }
}
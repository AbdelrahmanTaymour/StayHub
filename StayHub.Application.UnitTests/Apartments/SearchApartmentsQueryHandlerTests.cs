using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Apartments.SearchApartments;

namespace StayHub.Application.UnitTests.Apartments;

public class SearchApartmentsQueryHandlerTests
{
    private readonly SearchApartmentsQueryHandler _handler;
    private readonly ISqlConnectionFactory _sqlConnectionFactoryMock = Substitute.For<ISqlConnectionFactory>();

    public SearchApartmentsQueryHandlerTests()
    {
        _handler = new SearchApartmentsQueryHandler(_sqlConnectionFactoryMock);
    }

    [Theory]
    [InlineData(2026, 1, 10, 2026, 1, 1)] // Start after End
    [InlineData(2026, 1, 1, 2026, 1,
        1)] // Start equals End — zero-night search is treated as invalid, not just "no results"
    public async Task Handle_Should_ReturnEmptyList_WhenStartIsNotBeforeEnd(
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
        result.Value.Should().BeEmpty();
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
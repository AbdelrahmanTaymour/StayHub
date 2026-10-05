using FluentAssertions;
using MediatR;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Apartments.SearchApartments;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.UnitTests.Apartments;

public class SearchApartmentsQueryHandlerTests
{
    private readonly SearchApartmentsQueryHandler _handler;
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ISqlConnectionFactory _sqlConnectionFactoryMock = Substitute.For<ISqlConnectionFactory>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public SearchApartmentsQueryHandlerTests()
    {
        _handler = new SearchApartmentsQueryHandler(
            _sender,
            _sqlConnectionFactoryMock,
            _userContextMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCachedQueryFails()
    {
        // Arrange
        var error = Error.NotFound("Search.Failed", "Search failed.");

        _sender
            .Send(Arg.Any<CachedSearchApartmentsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PagedResponse<ApartmentSearchResult>>(error));

        // Act
        var result = await _handler.Handle(new SearchApartmentsQuery(), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public async Task Handle_Should_NotOpenDatabaseConnection_WhenCallerIsAnonymous()
    {
        // Arrange
        _userContextMock.IsAuthenticated.Returns(false);
        ArrangeCachedPage(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(new SearchApartmentsQuery(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(item => !item.IsFavorited);
        _sqlConnectionFactoryMock.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task Handle_Should_NotOpenDatabaseConnection_WhenCachedPageIsEmpty()
    {
        // Arrange
        _userContextMock.IsAuthenticated.Returns(true);
        ArrangeCachedPage();

        // Act
        var result = await _handler.Handle(new SearchApartmentsQuery(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        _sqlConnectionFactoryMock.DidNotReceive().CreateConnection();
    }

    private void ArrangeCachedPage(params Guid[] apartmentIds)
    {
        var items = apartmentIds
            .Select(id => new ApartmentSearchResult
            {
                Id = id,
                Name = "Apartment",
                City = "Cairo",
                Country = "Egypt",
                PricePerNight = 100m,
                Currency = "USD"
            })
            .ToList();

        var page = new PagedResponse<ApartmentSearchResult>
        {
            Items = items,
            Page = 1,
            PageSize = 20,
            TotalCount = items.Count,
            TotalPages = items.Count > 0 ? 1 : 0
        };

        _sender
            .Send(Arg.Any<CachedSearchApartmentsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(page));
    }
}
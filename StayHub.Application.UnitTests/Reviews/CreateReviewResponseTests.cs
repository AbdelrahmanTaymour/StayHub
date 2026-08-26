using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Reviews.CreateReviewResponse;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Application.UnitTests.Bookings;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Reviews;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Reviews;

public class CreateReviewResponseTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private static readonly Rating Rating = Rating.Create(5).Value;
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly CreateReviewResponseCommandHandler _handler;

    private readonly IReviewRepository _reviewRepositoryMock = Substitute.For<IReviewRepository>();

    private readonly IReviewResponseRepository _reviewResponseRepositoryMock =
        Substitute.For<IReviewResponseRepository>();

    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public CreateReviewResponseTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new CreateReviewResponseCommandHandler(
            _reviewRepositoryMock,
            _reviewResponseRepositoryMock,
            _apartmentRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static Review CreateReview(Apartment apartment) =>
        Review.Create(BookingData.ReserveConfirmAndComplete(apartment), Rating, new Comment("Great stay!"), UtcNow)
            .Value;

    private static CreateReviewResponseCommand CommandFor(Guid reviewId) => new(reviewId, "Thanks for staying!");

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenReviewNotFound()
    {
        // Arrange
        var reviewId = Guid.CreateVersion7();
        _reviewRepositoryMock.GetByIdAsync(reviewId, Arg.Any<CancellationToken>()).Returns((Review?)null);

        // Act
        var result = await _handler.Handle(CommandFor(reviewId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var review = CreateReview(apartment);
        _reviewRepositoryMock.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);
        _apartmentRepositoryMock.GetByIdAsync(review.ApartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(CommandFor(review.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange — includes the reviewing guest themselves, who is NOT
        // authorized to respond to their own review.
        var apartment = ApartmentData.Create();
        var review = CreateReview(apartment);
        _reviewRepositoryMock.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(review.UserId);
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(CommandFor(review.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewResponseErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenAlreadyResponded()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var review = CreateReview(apartment);
        var existingResponse = ReviewResponse.Create(review.Id, new Comment("Already responded"), UtcNow);
        _reviewRepositoryMock.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        _reviewResponseRepositoryMock.GetByReviewIdAsync(review.Id, Arg.Any<CancellationToken>())
            .Returns(existingResponse);

        // Act
        var result = await _handler.Handle(CommandFor(review.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewResponseErrors.AlreadyRespondedTo);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateResponseAndSaveChanges_WhenCallerIsOwner()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var review = CreateReview(apartment);
        _reviewRepositoryMock.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        _reviewResponseRepositoryMock.GetByReviewIdAsync(review.Id, Arg.Any<CancellationToken>())
            .Returns((ReviewResponse?)null);

        // Act
        var result = await _handler.Handle(CommandFor(review.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reviewResponseRepositoryMock.Received(1).Add(Arg.Is<ReviewResponse>(r =>
            r.Id == result.Value && r.ReviewId == review.Id));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateResponse_WhenCallerIsAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var review = CreateReview(apartment);
        _reviewRepositoryMock.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([Role.Admin.Name]);
        _reviewResponseRepositoryMock.GetByReviewIdAsync(review.Id, Arg.Any<CancellationToken>())
            .Returns((ReviewResponse?)null);

        // Act
        var result = await _handler.Handle(CommandFor(review.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
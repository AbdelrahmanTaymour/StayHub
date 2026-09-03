using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Reviews.CreateReviewResponse;
using StayHub.Domain.Reviews;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Reviews;

public class CreateReviewResponseTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateReviewResponse_ShouldPersistResponseAndEmailReviewer_ViaOutboxPipeline()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService);
        booking.Confirm(DateTime.UtcNow);
        booking.Complete(DateTime.UtcNow);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        var review = ReviewTestData.CreateReview(booking);
        DbContext.Add(review);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new CreateReviewResponseCommand(review.Id, "Thanks for the kind words!");

        // Act
        var result = await Sender.Send(command);
        result.IsSuccess.Should().BeTrue();

        await ProcessOutboxAsync();

        // Assert
        DbContext.ChangeTracker.Clear();
        var persistedResponse = await DbContext.Set<ReviewResponse>().SingleAsync(r => r.Id == result.Value);
        persistedResponse.ReviewId.Should().Be(review.Id);

        EmailService.SentEmails.Should().ContainSingle(e =>
            e.To.Value == guest.Email.Value &&
            e.Subject == "The owner replied to your review");
    }

    [Fact]
    public async Task CreateReviewResponse_ShouldReturnNotAuthorized_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService);
        booking.Confirm(DateTime.UtcNow);
        booking.Complete(DateTime.UtcNow);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        var review = ReviewTestData.CreateReview(booking);
        DbContext.Add(review);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new CreateReviewResponseCommand(review.Id, "I'll respond myself!");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewResponseErrors.NotAuthorized);
    }

    [Fact]
    public async Task CreateReviewResponse_ShouldReturnAlreadyRespondedTo_WhenReviewAlreadyHasAResponse()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService);
        booking.Confirm(DateTime.UtcNow);
        booking.Complete(DateTime.UtcNow);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        var review = ReviewTestData.CreateReview(booking);
        DbContext.Add(review);
        await DbContext.SaveChangesAsync();

        var existingResponse = ReviewResponse.Create(review.Id, new Comment("First response"), DateTime.UtcNow);
        DbContext.Add(existingResponse);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new CreateReviewResponseCommand(review.Id, "Second response");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewResponseErrors.AlreadyRespondedTo);
    }
}
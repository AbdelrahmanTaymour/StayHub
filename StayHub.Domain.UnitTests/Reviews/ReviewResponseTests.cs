using FluentAssertions;
using StayHub.Domain.Reviews;
using StayHub.Domain.Reviews.Events;
using StayHub.Domain.UnitTests.Infrastructure;

namespace StayHub.Domain.UnitTests.Reviews;

public class ReviewResponseTests : BaseTest
{
    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        // Arrange
        var reviewId = Guid.CreateVersion7();
        var comment = new Comment("Thanks for staying with us!");
        var utcNow = DateTime.UtcNow;

        // Act
        var response = ReviewResponse.Create(reviewId, comment, utcNow);

        // Assert
        response.ReviewId.Should().Be(reviewId);
        response.Comment.Should().Be(comment);
        response.CreatedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Create_Should_RaiseReviewResponseCreatedDomainEvent()
    {
        // Arrange
        var reviewId = Guid.CreateVersion7();

        // Act
        var response = ReviewResponse.Create(reviewId, new Comment("Thanks!"), DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ReviewResponseCreatedDomainEvent>(response);
        domainEvent.ReviewResponseId.Should().Be(response.Id);
        domainEvent.ReviewId.Should().Be(reviewId);
    }

    [Fact]
    public void UpdateComment_Should_SetComment()
    {
        // Arrange
        var response = ReviewResponse.Create(Guid.CreateVersion7(), new Comment("Original"), DateTime.UtcNow);
        var updatedComment = new Comment("Updated");

        // Act
        response.UpdateComment(updatedComment);

        // Assert
        response.Comment.Should().Be(updatedComment);
    }

    [Fact]
    public void UpdateComment_Should_NotRaiseAnyDomainEvent()
    {
        // Arrange
        var response = ReviewResponse.Create(Guid.CreateVersion7(), new Comment("Original"), DateTime.UtcNow);
        response.ClearDomainEvents();

        // Act
        response.UpdateComment(new Comment("Updated"));

        // Assert
        response.GetDomainEvents().Should().BeEmpty();
    }
}
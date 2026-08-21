using FluentAssertions;
using StayHub.Domain.Auditing;

namespace StayHub.Domain.UnitTests.Auditing;

public class AuditLogTests
{
    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var utcNow = DateTime.UtcNow;

        // Act
        var auditLog = AuditLog.Create(
            userId,
            "Apartment",
            "a1b2c3",
            AuditAction.Updated,
            "{\"Price\":{\"Old\":100,\"New\":120}}",
            utcNow);

        // Assert
        auditLog.UserId.Should().Be(userId);
        auditLog.EntityName.Should().Be("Apartment");
        auditLog.EntityId.Should().Be("a1b2c3");
        auditLog.Action.Should().Be(AuditAction.Updated);
        auditLog.Changes.Should().Be("{\"Price\":{\"Old\":100,\"New\":120}}");
        auditLog.OccurredOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Create_Should_AllowNullUserId_ForSystemInitiatedActions()
    {
        // Arrange & Act — e.g., CompleteExpiredBookingsJob acting with no
        // human user behind it.
        var auditLog = AuditLog.Create(
            userId: null,
            "Booking",
            "b1c2d3",
            AuditAction.Updated,
            "{\"Status\":{\"Old\":\"Confirmed\",\"New\":\"Completed\"}}",
            DateTime.UtcNow);

        // Assert
        auditLog.UserId.Should().BeNull();
    }
}
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Maintenance.CreateMaintenanceRequest;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Maintenance;

public class CreateMaintenanceRequestTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateMaintenanceRequest_ShouldEmailOwnerAndActiveStaff_ViaOutboxPipeline()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var staffUser = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, staffUser, apartment);
        await DbContext.SaveChangesAsync();

        var staffRole = Enum.GetValues<ApartmentStaffRole>().First();
        var assignment = ApartmentStaffAssignment.Create(apartment.Id, staffUser.Id, staffRole, DateTime.UtcNow);
        DbContext.Add(assignment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new CreateMaintenanceRequestCommand(apartment.Id, "Leaking faucet",
            "The kitchen faucet won't stop dripping.");

        // Act
        var result = await Sender.Send(command);
        result.IsSuccess.Should().BeTrue();

        await ProcessOutboxAsync();

        // Assert
        DbContext.ChangeTracker.Clear();
        var persistedRequest = await DbContext.Set<MaintenanceRequest>().SingleAsync(r => r.Id == result.Value);
        persistedRequest.Status.Should().Be(MaintenanceRequestStatus.Open);

        EmailService.SentEmails.Should().Contain(e =>
            e.To.Value == owner.Email.Value && e.Subject == "New maintenance request");
        EmailService.SentEmails.Should().Contain(e =>
            e.To.Value == staffUser.Email.Value && e.Subject == "New maintenance request");
    }

    [Fact]
    public async Task
        CreateMaintenanceRequest_ShouldReturnNotAuthorized_WhenCallerHasNoActiveBookingAndIsNotOwnerOrAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var unrelatedUser = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, unrelatedUser, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(unrelatedUser.Id, Role.Guest.Name);

        var command = new CreateMaintenanceRequestCommand(apartment.Id, "Leaking faucet",
            "The kitchen faucet won't stop dripping.");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task CreateMaintenanceRequest_ShouldSucceed_WhenCallerIsATenantWithAnActiveBooking()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var tenant = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, tenant, apartment);
        await DbContext.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var booking = BookingTestData.Reserve(apartment, tenant.Id, today, today.AddDays(5), PricingService);
        booking.Confirm(DateTime.UtcNow);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(tenant.Id, Role.Guest.Name);

        var command = new CreateMaintenanceRequestCommand(apartment.Id, "No hot water", "The shower only runs cold.");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
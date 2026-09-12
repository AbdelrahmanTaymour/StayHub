using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Domain.Bookings;
using StayHub.Infrastructure;

namespace StayHub.Api.FunctionalTests.Reviews;

internal static class ReviewTestFixtures
{
    public static async Task CompleteBookingDirectlyAsync(FunctionalTestWebAppFactory factory, Guid bookingId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var booking = await dbContext.Set<Booking>().FirstAsync(b => b.Id == bookingId);
        booking.Complete(DateTime.UtcNow);

        await dbContext.SaveChangesAsync();
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Domain.Bookings;
using StayHub.Infrastructure;

namespace StayHub.Api.FunctionalTests.Reviews;

internal static class ReviewTestData
{
    public static object ValidCreateRequest(Guid bookingId, int rating = 5, string? comment = null)
    {
        return new
        {
            BookingId = bookingId,
            Rating = rating,
            Comment = comment ?? "Wonderful stay, would book again."
        };
    }

    public static object ValidResponseRequest(string? comment = null)
    {
        return new { Comment = comment ?? "Thank you for staying with us!" };
    }

    public static async Task CompleteBookingDirectlyAsync(FunctionalTestWebAppFactory factory, Guid bookingId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var booking = await dbContext.Set<Booking>().FirstAsync(b => b.Id == bookingId);
        booking.Complete(DateTime.UtcNow);

        await dbContext.SaveChangesAsync();
    }
}
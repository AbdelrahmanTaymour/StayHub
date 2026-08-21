using FluentAssertions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.UnitTests.Apartments;
using StayHub.Domain.UnitTests.Infrastructure;

namespace StayHub.Domain.UnitTests.Bookings;

public class BookingTests : BaseTest
{
    [Fact]
    public void Reserve_Should_ReturnSuccessAndSetPropertyValues()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var userId = Guid.CreateVersion7();

        // Act
        var result = Booking.Reserve(apartment, userId, BookingData.Duration, new PricingService(), DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApartmentId.Should().Be(apartment.Id);
        result.Value.UserId.Should().Be(userId);
        result.Value.Duration.Should().Be(BookingData.Duration);
        result.Value.Status.Should().Be(BookingStatus.Reserved);
    }

    [Fact]
    public void Reserve_Should_RaiseBookingReservedDomainEvent()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        // Act
        var booking = BookingData.Reserve(apartment);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<BookingReservedDomainEvent>(booking);
        domainEvent.BookingId.Should().Be(booking.Id);
    }

    [Fact]
    public void Reserve_Should_SetPriceFieldsFromPricingService()
    {
        // Arrange — sanity check that Reserve uses the pricing service's output
        var apartment = ApartmentData.Create();
        var pricingService = new PricingService();
        var expected = pricingService.CalculatePrice(apartment, BookingData.Duration);

        // Act
        var result = Booking.Reserve(apartment, Guid.CreateVersion7(), BookingData.Duration, pricingService,
            DateTime.UtcNow);

        // Assert
        result.Value.PriceForPeriod.Should().Be(expected.PriceForPeriod);
        result.Value.CleaningFee.Should().Be(expected.CleaningFee);
        result.Value.AmenitiesUpCharge.Should().Be(expected.AmenitiesUpCharge);
        result.Value.TotalPrice.Should().Be(expected.TotalPrice);
    }

    [Fact]
    public void Reserve_Should_ReturnFailure_WhenDurationIsZero()
    {
        // Arrange — a same-day (0-night) range is a valid DateRange
        // (Create's guard is "start > end", not ">="), but Reserve
        // explicitly rejects it as a bookable stay.
        var apartment = ApartmentData.Create();
        var sameDay = new DateOnly(2026, 1, 1);
        var zeroLengthDuration = DateRange.Create(sameDay, sameDay);

        // Act
        var result = Booking.Reserve(
            apartment, Guid.CreateVersion7(), zeroLengthDuration, new PricingService(), DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.InvalidDuration);
    }

    [Fact]
    public void Reserve_Should_NotRaiseAnyDomainEvent_WhenDurationIsZero()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var sameDay = new DateOnly(2026, 1, 1);
        var zeroLengthDuration = DateRange.Create(sameDay, sameDay);

        // Act
        Booking.Reserve(apartment, Guid.CreateVersion7(), zeroLengthDuration, new PricingService(), DateTime.UtcNow);

        // Assert 
        apartment.GetDomainEvents().OfType<BookingReservedDomainEvent>().Should().BeEmpty();
    }

    [Fact]
    public void Confirm_Should_SetStatusConfirmedAndReturnSuccess_WhenReserved()
    {
        // Arrange
        var booking = BookingData.Reserve();
        var utcNow = DateTime.UtcNow;

        // Act
        var result = booking.Confirm(utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ConfirmedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Confirm_Should_RaiseBookingConfirmedDomainEvent_WhenReserved()
    {
        // Arrange
        var booking = BookingData.Reserve();

        // Act
        booking.Confirm(DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<BookingConfirmedDomainEvent>(booking);
        domainEvent.BookingId.Should().Be(booking.Id);
    }

    [Fact]
    public void Confirm_Should_ReturnFailure_WhenBookingStatusIsNotReserved()
    {
        // Arrange
        var booking = BookingData.ReserveAndConfirm();

        // Act - before act the booking status is already "Confirmed",
        // and "Confirm" only make sense with "Reserved" booking status
        var result = booking.Confirm(DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotReserved);
    }

    [Fact]
    public void Reject_Should_SetStatusRejectedAndReturnSuccess_WhenReserved()
    {
        // Arrange
        var booking = BookingData.Reserve();
        var rejectedByUserId = Guid.CreateVersion7();
        var utcNow = DateTime.UtcNow;

        // Act
        var result = booking.Reject(rejectedByUserId, utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.RejectedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Reject_Should_RaiseBookingRejectedDomainEvent_WithRejectedByUserId()
    {
        // Arrange
        var booking = BookingData.Reserve();
        var rejectedByUserId = Guid.CreateVersion7();

        // Act
        booking.Reject(rejectedByUserId, DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<BookingRejectedDomainEvent>(booking);
        domainEvent.BookingId.Should().Be(booking.Id);
        domainEvent.RejectedByUserId.Should().Be(rejectedByUserId);
    }

    [Fact]
    public void Reject_Should_ReturnFailure_WhenAlreadyConfirmed()
    {
        // Arrange — a confirmed booking is past the "reject a pending
        // request" stage entirely.
        var booking = BookingData.ReserveAndConfirm();

        // Act
        var result = booking.Reject(Guid.CreateVersion7(), DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotReserved);
    }

    [Fact]
    public void Complete_Should_SetStatusCompletedAndReturnSuccess()
    {
        // Arrange
        var booking = BookingData.ReserveAndConfirm();
        var utcNow = DateTime.UtcNow;

        // Act
        var result = booking.Complete(utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Completed);
        booking.CompletedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Complete_Should_RaiseBookingCompletedDomainEvent()
    {
        // Arrange
        var booking = BookingData.ReserveAndConfirm();

        // Act
        booking.Complete(DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<BookingCompletedDomainEvent>(booking);
        domainEvent.BookingId.Should().Be(booking.Id);
    }


    [Fact]
    public void Complete_Should_ReturnFailure_WhenStillReserved()
    {
        // Arrange — a booking can't be completed before it's even confirmed.
        var booking = BookingData.Reserve();

        // Act
        var result = booking.Complete(DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotConfirmed);
    }

    [Fact]
    public void Cancel_Should_SucceedForReservedBooking_WhenBeforeStartDate()
    {
        // Arrange — Duration starts 2026-01-01; "now" here is before that.
        var booking = BookingData.Reserve();
        var beforeStart = new DateTime(2025, 12, 31);

        // Act
        var result = booking.Cancel(beforeStart);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancelledOnUtc.Should().Be(beforeStart);
    }

    [Fact]
    public void Cancel_Should_SucceedForConfirmedBooking_WhenBeforeStartDate()
    {
        // Arrange
        var booking = BookingData.ReserveAndConfirm();
        var beforeStart = new DateTime(2025, 12, 31);

        // Act
        var result = booking.Cancel(beforeStart);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Should_Succeed_WhenCancelledExactlyOnStartDate()
    {
        // Arrange — boundary: the guard is "currentDate > Duration.Start",
        // not ">=", so cancelling on the start date itself is still allowed.
        var booking = BookingData.ReserveAndConfirm();
        var onStartDate = new DateTime(2026, 1, 1);

        // Act
        var result = booking.Cancel(onStartDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Cancel_Should_ReturnFailure_WhenBookingAlreadyStarted()
    {
        // Arrange — the day after Duration.Start (2026-01-01).
        var booking = BookingData.ReserveAndConfirm();
        var afterStart = new DateTime(2026, 1, 2);

        // Act
        var result = booking.Cancel(afterStart);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.AlreadyStarted);
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Theory]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Cancelled)]
    public void Cancel_Should_ReturnFailure_WhenStatusIsNotCancellable(BookingStatus status)
    {
        // Arrange — get a booking into each terminal, non-cancellable status.
        var booking = status switch
        {
            BookingStatus.Rejected => BookingData.RejectedBooking(),
            BookingStatus.Completed => BookingData.CompletedBooking(),
            BookingStatus.Cancelled => BookingData.AlreadyCancelledBooking(),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        // Act
        var result = booking.Cancel(new DateTime(2025, 12, 31));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotCancellable);
    }

    [Fact]
    public void Cancel_Should_RaiseBookingCancelledDomainEvent_WhenSuccessful()
    {
        // Arrange
        var booking = BookingData.ReserveAndConfirm();

        // Act
        booking.Cancel(new DateTime(2025, 12, 31));

        // Assert
        var domainEvent = AssertDomainEventWasPublished<BookingCancelledDomainEvent>(booking);
        domainEvent.BookingId.Should().Be(booking.Id);
    }
}
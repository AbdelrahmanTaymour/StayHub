using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Application.Abstractions.Payments;

namespace StayHub.Api.FunctionalTests.Payments;

public sealed class InitiatePaymentTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<(string OwnerToken, string GuestToken, Guid BookingId)> ArrangeConfirmedBookingAsync()
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        AuthenticateAs(ownerToken);
        var confirmResponse = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);
        confirmResponse.EnsureSuccessStatusCode();

        return (ownerToken, guestToken, bookingId);
    }

    [Fact]
    public async Task Initiate_ShouldReturnCreatedWithClientSecret_WhenBookingIsConfirmedAndCallerIsTheGuest()
    {
        // Arrange
        var (_, guestToken, bookingId) = await ArrangeConfirmedBookingAsync();
        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(PaymentRoutes.BaseRoute, new { BookingId = bookingId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("paymentId").GetGuid().Should().NotBe(Guid.Empty);
        body.GetProperty("clientSecret").GetString().Should().NotBeNullOrWhiteSpace();
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Initiate_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var request = new { BookingId = Guid.NewGuid() };

        // Act
        var response = await HttpClient.PostAsJsonAsync(PaymentRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Initiate_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(PaymentRoutes.BaseRoute, new { BookingId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Initiate_ShouldReturnForbidden_WhenCallerIsNotTheBookingGuest()
    {
        // Arrange
        var (ownerToken, _, bookingId) = await ArrangeConfirmedBookingAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(PaymentRoutes.BaseRoute, new { BookingId = bookingId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Payment.NotAuthorized");
    }

    [Fact]
    public async Task Initiate_ShouldReturnConflict_WhenBookingIsNotYetConfirmed()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();
        // Deliberately left Reserved - never confirmed.

        // Act
        var response = await HttpClient.PostAsJsonAsync(PaymentRoutes.BaseRoute, new { BookingId = bookingId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Payment.BookingNotConfirmed");
    }

    [Fact]
    public async Task Initiate_ShouldReturnConflict_WhenAPendingPaymentAlreadyExistsForTheBooking()
    {
        // Arrange
        var (_, guestToken, bookingId) = await ArrangeConfirmedBookingAsync();
        AuthenticateAs(guestToken);
        var firstInitiate = await HttpClient.PostAsJsonAsync(PaymentRoutes.BaseRoute, new { BookingId = bookingId });
        firstInitiate.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsJsonAsync(PaymentRoutes.BaseRoute, new { BookingId = bookingId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Payment.AlreadyInitiated");
    }

    [Fact]
    public async Task Initiate_ShouldAllowRetry_WhenThePreviousPaymentAttemptFailed()
    {
        // GetActiveByBookingIdAsync's own contract: a Failed payment does NOT count as active,
        // so the guest whose card was declined can try again.
        // Flow:
        // (1) initiate through the public API,
        // (2) read the generated provider reference from the payment gateway,
        // (3) construct a genuinely valid signed webhook payload,
        // (4) send it to the real webhook endpoint,
        // (5) assert the observable result - no part of the Succeeded/Failed transition itself is bypassed.

        // Arrange
        var (_, guestToken, bookingId) = await ArrangeConfirmedBookingAsync();
        AuthenticateAs(guestToken);
        var firstInitiate = await HttpClient.PostAsJsonAsync(PaymentRoutes.BaseRoute, new { BookingId = bookingId });
        firstInitiate.EnsureSuccessStatusCode();
        var firstBody = await firstInitiate.Content.ReadFromJsonAsync<JsonElement>();
        var firstPaymentId = firstBody.GetProperty("paymentId").GetGuid();

        using var scope = Factory.Services.CreateScope();
        var gatewayService = (TestPaymentGatewayService)scope.ServiceProvider
            .GetRequiredService<IPaymentGatewayService>();
        var providerReference = gatewayService.CreatedPaymentIntents.Single().ProviderReference;

        var payload = StripeEventPayloads.PaymentIntentFailed(providerReference);
        var signature = StripeWebhookSigner.Sign(payload);
        var webhookContent = new StringContent(payload, Encoding.UTF8, "application/json");
        webhookContent.Headers.Add("Stripe-Signature", signature);

        HttpClient.DefaultRequestHeaders.Authorization = null; // webhook is AllowAnonymous
        var webhookResponse = await HttpClient.PostAsync(PaymentRoutes.Webhook, webhookContent);
        webhookResponse.EnsureSuccessStatusCode();

        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(PaymentRoutes.BaseRoute, new { BookingId = bookingId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("paymentId").GetGuid().Should().NotBe(firstPaymentId);
    }
}
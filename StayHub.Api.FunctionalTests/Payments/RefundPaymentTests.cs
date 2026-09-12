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

public sealed class RefundPaymentTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<(string OwnerToken, string GuestToken, Guid PaymentId)> ArrangeSucceededPaymentAsync()
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        AuthenticateAs(ownerToken);
        var confirmResponse = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);
        confirmResponse.EnsureSuccessStatusCode();

        AuthenticateAs(guestToken);
        var initiateResponse = await HttpClient.PostAsJsonAsync(PaymentRoutes.BaseRoute, new { BookingId = bookingId });
        initiateResponse.EnsureSuccessStatusCode();
        var initiateBody = await initiateResponse.Content.ReadFromJsonAsync<JsonElement>();
        var paymentId = initiateBody.GetProperty("paymentId").GetGuid();

        using var scope = Factory.Services.CreateScope();
        var gatewayService = (TestPaymentGatewayService)scope.ServiceProvider
            .GetRequiredService<IPaymentGatewayService>();
        var providerReference = gatewayService.CreatedPaymentIntents.Single().ProviderReference;

        var payload = StripeEventPayloads.PaymentIntentSucceeded(providerReference);
        var signature = StripeWebhookSigner.Sign(payload);
        var webhookContent = new StringContent(payload, Encoding.UTF8, "application/json");
        webhookContent.Headers.Add("Stripe-Signature", signature);
        HttpClient.DefaultRequestHeaders.Authorization = null;
        var webhookResponse = await HttpClient.PostAsync(PaymentRoutes.Webhook, webhookContent);
        webhookResponse.EnsureSuccessStatusCode();

        return (ownerToken, guestToken, paymentId);
    }

    [Fact]
    public async Task Refund_ShouldReturnNoContent_WhenCallerIsTheGuest()
    {
        // Arrange
        var (_, guestToken, paymentId) = await ArrangeSucceededPaymentAsync();
        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.PostAsync(PaymentRoutes.Refund(paymentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refund_ShouldReturnNoContent_WhenCallerIsTheApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, paymentId) = await ArrangeSucceededPaymentAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsync(PaymentRoutes.Refund(paymentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refund_ShouldReturnNoContent_WhenCallerIsAdmin()
    {
        // Arrange
        var (_, _, paymentId) = await ArrangeSucceededPaymentAsync();

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.PostAsync(PaymentRoutes.Refund(paymentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refund_ShouldReturnForbidden_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (_, _, paymentId) = await ArrangeSucceededPaymentAsync();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsync(PaymentRoutes.Refund(paymentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Payment.NotAuthorized");
    }

    [Fact]
    public async Task Refund_ShouldReturnNotFound_WhenPaymentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsync(PaymentRoutes.Refund(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Refund_ShouldReturnConflict_WhenPaymentHasNotSucceeded()
    {
        // A Pending payment (never confirmed via webhook) cannot be refunded.
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        AuthenticateAs(ownerToken);
        var confirmResponse = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);
        confirmResponse.EnsureSuccessStatusCode();

        AuthenticateAs(guestToken);
        var initiateResponse = await HttpClient.PostAsJsonAsync(PaymentRoutes.BaseRoute, new { BookingId = bookingId });
        initiateResponse.EnsureSuccessStatusCode();
        var initiateBody = await initiateResponse.Content.ReadFromJsonAsync<JsonElement>();
        var paymentId = initiateBody.GetProperty("paymentId").GetGuid();
        // Deliberately never sent the webhook - payment stays Pending.

        // Act
        var response = await HttpClient.PostAsync(PaymentRoutes.Refund(paymentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Payment.NotSucceeded");
    }

    [Fact]
    public async Task Refund_ShouldReturnConflict_WhenPaymentIsAlreadyRefunded()
    {
        // Arrange
        var (_, guestToken, paymentId) = await ArrangeSucceededPaymentAsync();
        AuthenticateAs(guestToken);
        var firstRefund = await HttpClient.PostAsync(PaymentRoutes.Refund(paymentId), null);
        firstRefund.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsync(PaymentRoutes.Refund(paymentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Payment.AlreadyRefunded");
    }

    [Fact]
    public async Task Refund_ShouldCallGatewayRefund_WhenSuccessful()
    {
        // Confirms RefundPaymentCommandHandler actually calls IPaymentGatewayService.RefundAsync,
        // not just the domain-level status transition.
        // Arrange
        var (_, guestToken, paymentId) = await ArrangeSucceededPaymentAsync();
        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.PostAsync(PaymentRoutes.Refund(paymentId), null);
        response.EnsureSuccessStatusCode();

        // Assert
        using var scope = Factory.Services.CreateScope();
        var gatewayService = (TestPaymentGatewayService)scope.ServiceProvider
            .GetRequiredService<IPaymentGatewayService>();
        gatewayService.IssuedRefunds.Should().ContainSingle();
    }

    [Fact]
    public async Task Refund_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsync(PaymentRoutes.Refund(paymentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
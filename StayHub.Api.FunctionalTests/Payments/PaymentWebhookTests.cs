using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Application.Abstractions.Payments;

namespace StayHub.Api.FunctionalTests.Payments;

public sealed class PaymentWebhookTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<string> ArrangeInitiatedPaymentAsync()
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

        using var scope = Factory.Services.CreateScope();
        var gatewayService = (TestPaymentGatewayService)scope.ServiceProvider
            .GetRequiredService<IPaymentGatewayService>();

        return gatewayService.CreatedPaymentIntents.Single().ProviderReference;
    }

    private static HttpRequestMessage BuildWebhookRequest(string payload, string signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, PaymentRoutes.Webhook)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", signature);
        return request;
    }

    [Fact]
    public async Task Webhook_ShouldReturnOk_WhenSignatureIsValidAndEventIsPaymentIntentSucceeded()
    {
        // Arrange
        var providerReference = await ArrangeInitiatedPaymentAsync();
        HttpClient.DefaultRequestHeaders.Authorization = null;
        var payload = StripeEventPayloads.PaymentIntentSucceeded(providerReference);
        var signature = StripeWebhookSigner.Sign(payload);

        // Act
        var response = await HttpClient.SendAsync(BuildWebhookRequest(payload, signature));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Webhook_ShouldReturnOk_WhenSignatureIsValidAndEventIsPaymentIntentFailed()
    {
        // Arrange
        var providerReference = await ArrangeInitiatedPaymentAsync();
        HttpClient.DefaultRequestHeaders.Authorization = null;
        var payload = StripeEventPayloads.PaymentIntentFailed(providerReference);
        var signature = StripeWebhookSigner.Sign(payload);

        // Act
        var response = await HttpClient.SendAsync(BuildWebhookRequest(payload, signature));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Webhook_ShouldReturnBadRequest_WhenSignatureIsInvalid()
    {
        // Arrange
        var providerReference = await ArrangeInitiatedPaymentAsync();
        HttpClient.DefaultRequestHeaders.Authorization = null;
        var payload = StripeEventPayloads.PaymentIntentSucceeded(providerReference);
        var invalidSignature = StripeWebhookSigner.Sign(payload, secretOverride: "whsec_completely_wrong_secret");

        // Act
        var response = await HttpClient.SendAsync(BuildWebhookRequest(payload, invalidSignature));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_ShouldReturnBadRequest_WhenSignatureHeaderIsMissing()
    {
        // Arrange
        var providerReference = await ArrangeInitiatedPaymentAsync();
        HttpClient.DefaultRequestHeaders.Authorization = null;
        var payload = StripeEventPayloads.PaymentIntentSucceeded(providerReference);
        var request = new HttpRequestMessage(HttpMethod.Post, PaymentRoutes.Webhook)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        // Deliberately no Stripe-Signature header.

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_ShouldReturnBadRequest_WhenPayloadWasTamperedWithAfterSigning()
    {
        // Arrange
        var providerReference = await ArrangeInitiatedPaymentAsync();
        HttpClient.DefaultRequestHeaders.Authorization = null;
        var originalPayload = StripeEventPayloads.PaymentIntentSucceeded(providerReference);
        var signature = StripeWebhookSigner.Sign(originalPayload);
        var tamperedPayload = StripeEventPayloads.PaymentIntentSucceeded("pi_test_someone_elses_payment");

        // Act
        var response = await HttpClient.SendAsync(BuildWebhookRequest(tamperedPayload, signature));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_ShouldReturnOk_WhenEventTypeIsNotHandled()
    {
        // PaymentEndpoints.Webhook's switch statement only handles two event types; anything
        // else falls through with no action and still returns 200 - matching Stripe's guidance
        // to acknowledge receipt of events you don't act on, rather than erroring.

        // Arrange
        var providerReference = await ArrangeInitiatedPaymentAsync();
        HttpClient.DefaultRequestHeaders.Authorization = null;
        var payload = StripeEventPayloads.UnhandledEventType(providerReference);
        var signature = StripeWebhookSigner.Sign(payload);

        // Act
        var response = await HttpClient.SendAsync(BuildWebhookRequest(payload, signature));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Webhook_ShouldReturnOk_WhenProviderReferenceMatchesNoPayment()
    {
        // Arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        var payload = StripeEventPayloads.PaymentIntentSucceeded("pi_test_does_not_exist");
        var signature = StripeWebhookSigner.Sign(payload);

        // Act
        var response = await HttpClient.SendAsync(BuildWebhookRequest(payload, signature));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Webhook_ShouldReturnOk_WhenMarkingAnAlreadySucceededPaymentAgain()
    {
        // Arrange
        var providerReference = await ArrangeInitiatedPaymentAsync();
        HttpClient.DefaultRequestHeaders.Authorization = null;
        var payload = StripeEventPayloads.PaymentIntentSucceeded(providerReference);
        var firstSignature = StripeWebhookSigner.Sign(payload);
        var firstResponse = await HttpClient.SendAsync(BuildWebhookRequest(payload, firstSignature));
        firstResponse.EnsureSuccessStatusCode();

        var secondSignature = StripeWebhookSigner.Sign(payload);

        // Act
        var response = await HttpClient.SendAsync(BuildWebhookRequest(payload, secondSignature));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
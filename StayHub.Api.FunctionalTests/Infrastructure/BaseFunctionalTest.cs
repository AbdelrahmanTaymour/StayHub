using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StayHub.Api.Endpoints.Users;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Payments;

namespace StayHub.Api.FunctionalTests.Infrastructure;

[CollectionDefinition(nameof(FunctionalTestCollection))]
public class FunctionalTestCollection : ICollectionFixture<FunctionalTestWebAppFactory>;

[Collection(nameof(FunctionalTestCollection))]
public abstract class BaseFunctionalTest : IAsyncLifetime
{
    protected readonly FunctionalTestWebAppFactory Factory;
    protected readonly HttpClient HttpClient;

    protected BaseFunctionalTest(FunctionalTestWebAppFactory factory)
    {
        Factory = factory;
        HttpClient = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(Factory.ResetDatabaseAsync(), Factory.ResetCacheAsync());

        var paymentGatewayService = (TestPaymentGatewayService)Factory.Services
            .GetRequiredService<IPaymentGatewayService>();
        paymentGatewayService.Reset();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task<(string AccessToken, RegisterUserRequest Request, Guid UserId)> RegisterAndAuthenticateAsync()
    {
        var request = new RegisterUserRequest(
            "Test",
            "User",
            $"{Guid.NewGuid():N}@test.local",
            "Str0ng!Passw0rd");

        var registerResponse = await HttpClient.PostAsJsonAsync("api/v1/users/register", request);
        registerResponse.EnsureSuccessStatusCode();

        var userId = await registerResponse.Content.ReadFromJsonAsync<Guid>();

        var loginResponse = await HttpClient.PostAsJsonAsync(
            "api/v1/users/login",
            new LogInUserRequest(request.Email, request.Password));
        loginResponse.EnsureSuccessStatusCode();

        var accessTokenResponse = await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();

        return (accessTokenResponse!.AccessToken, request, userId);
    }

    protected void AuthenticateAs(string accessToken)
    {
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
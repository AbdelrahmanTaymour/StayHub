using MediatR;
using Microsoft.Extensions.DependencyInjection;
using StayHub.Application.Abstractions.BackgroundJobs;
using StayHub.Application.Abstractions.Caching;
using StayHub.Application.Abstractions.Email;
using StayHub.Application.Abstractions.Payments;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Bookings;
using StayHub.Infrastructure;
using StayHub.Infrastructure.Outbox;

namespace StayHub.Application.IntegrationTests.Integration;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory>;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest : IAsyncLifetime, IDisposable
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly IServiceScope _scope;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _scope = factory.Services.CreateScope();

        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        DbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        UserContext = _scope.ServiceProvider.GetRequiredService<TestUserContext>();
        EmailService = (TestEmailService)_scope.ServiceProvider.GetRequiredService<IEmailService>();
        BackgroundJobScheduler =
            (TestBackgroundJobScheduler)_scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();
        PaymentGatewayService =
            (TestPaymentGatewayService)_scope.ServiceProvider.GetRequiredService<IPaymentGatewayService>();
        FileStorageService =
            (TestFileStorageService)_scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        CacheService = _scope.ServiceProvider.GetRequiredService<ICacheService>();
        PricingService = _scope.ServiceProvider.GetRequiredService<PricingService>();
        SaveChangesInterceptor = _scope.ServiceProvider.GetRequiredService<TestFailingSaveChangesInterceptor>();
    }

    protected ISender Sender { get; }
    protected ApplicationDbContext DbContext { get; }
    protected TestUserContext UserContext { get; }
    protected TestEmailService EmailService { get; }
    protected TestBackgroundJobScheduler BackgroundJobScheduler { get; }
    protected TestPaymentGatewayService PaymentGatewayService { get; }
    protected TestFileStorageService FileStorageService { get; }
    protected ICacheService CacheService { get; }
    protected PricingService PricingService { get; }
    protected TestFailingSaveChangesInterceptor SaveChangesInterceptor { get; }

    // Runs once before each test method — keeps tests order-independent
    // without requiring a fresh container per class. Resets both real
    // infrastructure stores: Postgres (Respawn) and Redis (FLUSHDB) — several
    // Apartment queries implement ICachedQuery and are cached through a real
    // QueryCachingBehavior, so a stale Redis entry from an earlier test could
    // otherwise leak into a later test that builds the same cache key.
    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        await _factory.ResetCacheAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        _scope.Dispose();
    }

    protected Task ProcessOutboxAsync()
    {
        var job = _scope.ServiceProvider.GetRequiredService<ProcessOutboxMessagesJob>();

        return job.ProcessAsync(TestJobCancellationToken.Instance);
    }

    /// <summary>
    /// Mutates the scoped TestUserContext so subsequent Sender.Send calls in
    /// this test execute as the given user/role set (e.g. "as the owner",
    /// "as an unrelated user", "as a guest").
    /// </summary>
    protected void SetCurrentUser(Guid userId, params string[] roles)
    {
        UserContext.UserId = userId;
        UserContext.IdentityId = userId.ToString();
        UserContext.Roles = roles;
    }
}
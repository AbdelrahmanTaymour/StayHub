using MediatR;
using Microsoft.Extensions.Logging;
using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Abstractions.Behaviors;

internal sealed class QueryCachingBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, Result<TResponse>>
    where TRequest : ICachedQuery<TResponse>
{
    public async Task<Result<TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<Result<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (!request.IsCacheable)
        {
            return await next(cancellationToken);
        }

        var cacheKey = await GetCacheKeyAsync(request, cancellationToken);

        var cachedValue = await cacheService.GetAsync<TResponse>(
            cacheKey,
            cancellationToken);

        var requestName = typeof(TRequest).Name;

        if (cachedValue is not null)
        {
            logger.LogDebug("Cache hit for {RequestName}", requestName);

            return Result.Success(cachedValue);
        }

        logger.LogInformation("Cache miss for {RequestName}", requestName);

        var result = await next(cancellationToken);

        if (result.IsSuccess)
        {
            await cacheService.SetAsync(
                cacheKey,
                result.Value,
                request.Expiration,
                cancellationToken);
        }

        return result;
    }

    private async Task<string> GetCacheKeyAsync(
        TRequest request,
        CancellationToken cancellationToken)
    {
        if (request is not IVersionedCachedQuery<TResponse> versionedQuery)
        {
            return request.CacheKey;
        }

        var version = await cacheService.GetAsync<long?>(
            versionedQuery.VersionKey,
            cancellationToken) ?? 0;

        return $"{request.CacheKey}:v{version}";
    }
}
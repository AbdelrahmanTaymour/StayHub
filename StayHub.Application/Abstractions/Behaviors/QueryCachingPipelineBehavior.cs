using MediatR;
using Microsoft.Extensions.Logging;
using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Abstractions.Behaviors;

internal sealed class QueryCachingPipelineBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<QueryCachingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, Result<TResponse>>
    where TRequest : ICachedQuery<TResponse>
{
    public async Task<Result<TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<Result<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var cachedValue = await cacheService.GetAsync<TResponse>(request.CacheKey, cancellationToken);

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
            await cacheService.SetAsync(request.CacheKey, result.Value, request.Expiration, cancellationToken);
        }

        return result;
    }
}
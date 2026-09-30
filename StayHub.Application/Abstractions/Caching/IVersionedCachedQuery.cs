namespace StayHub.Application.Abstractions.Caching;

public interface IVersionedCachedQuery<TResponse> : ICachedQuery<TResponse>
{
    string VersionKey { get; }
}
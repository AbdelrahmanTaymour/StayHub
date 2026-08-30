namespace StayHub.Infrastructure.Storage;

internal interface IImageProcessor
{
    Task<ProcessedImage> ProcessAsync(Stream original, CancellationToken cancellationToken = default);
}
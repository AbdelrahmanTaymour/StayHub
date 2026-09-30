using StayHub.Application.Abstractions.Storage;

namespace StayHub.Infrastructure.Storage;

internal interface IImageProcessor
{
    /// <summary>
    ///     Re-encodes <paramref name="original"/> per the resize/quality/watermark rules for
    ///     <paramref name="category"/> - see ImageProcessor for the actual numbers and rationale.
    ///     Always returns a fresh, fully buffered stream positioned at 0; the caller (
    ///     ObjectStorageService) uploads it as-is.
    /// </summary>
    Task<ProcessedImage> ProcessAsync(
        Stream original,
        ImageCategory category,
        CancellationToken cancellationToken = default);
}
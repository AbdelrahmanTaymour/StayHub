namespace StayHub.Infrastructure.Storage;

public class ImageProcessor : IImageProcessor
{
    public Task<ProcessedImage> ProcessAsync(Stream original, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
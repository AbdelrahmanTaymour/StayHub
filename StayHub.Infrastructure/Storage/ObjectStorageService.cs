using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using StayHub.Application.Abstractions.Storage;

namespace StayHub.Infrastructure.Storage;

internal sealed class ObjectStorageService(
    IAmazonS3 s3Client,
    IImageProcessor imageProcessor,
    IOptions<StorageSettings> storageSettings) : IFileStorageService
{
    private readonly StorageSettings _settings = storageSettings.Value;

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        ImageCategory category,
        CancellationToken cancellationToken = default)
    {
        var processed = await imageProcessor.ProcessAsync(content, category, cancellationToken);

        var key = $"{KeyPrefix(category)}/{Guid.NewGuid()}{processed.FileExtension}";

        await using (processed.Content)
        {
            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                InputStream = processed.Content,
                ContentType = processed.ContentType,
                AutoCloseStream = false, // Handled by async using block
                DisablePayloadSigning = true
            };

            await s3Client.PutObjectAsync(request, cancellationToken);
        }

        return key;
    }

    public Task<string> GeneratePresignedUrlAsync(string key, CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_settings.PresignedUrlExpirationMinutes)
        };

        var url = s3Client.GetPreSignedURL(request);

        return Task.FromResult(url);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key
        };

        await s3Client.DeleteObjectAsync(request, cancellationToken);
    }

    private static string KeyPrefix(ImageCategory category) => category switch
    {
        ImageCategory.ApartmentPhoto => "apartments",
        ImageCategory.UserAvatar => "avatars",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, $"Unhandled image category: {category}")
    };
}
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
        CancellationToken cancellationToken = default)
    {
        var processed = await imageProcessor.ProcessAsync(content, cancellationToken);

        var key = $"apartments/{Guid.NewGuid()}{processed.FileExtension}";

        var request = new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            InputStream = processed.Content,
            ContentType = processed.ContentType,
            AutoCloseStream = true,
            // Many S3-compatible providers (Backblaze B2, some MinIO setups) don't support the AWS
            // SDK's default chunked/streaming SigV4 signing for uploads. Disabling it falls back to a
            // single upfront SHA256 hash instead, which is the broadly-compatible option across providers.
            DisablePayloadSigning = true
        };

        await s3Client.PutObjectAsync(request, cancellationToken);

        return $"{_settings.PublicBaseUrl.TrimEnd('/')}/{key}";
    }

    public async Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        var key = ExtractKeyFromUrl(url);

        await s3Client.DeleteObjectAsync(_settings.BucketName, key, cancellationToken);
    }

    private static string ExtractKeyFromUrl(string url)
    {
        // Reverses the URL format built in UploadAsync: everything after the public base host is the object key.
        var uri = new Uri(url);
        return uri.AbsolutePath.TrimStart('/');
    }
}
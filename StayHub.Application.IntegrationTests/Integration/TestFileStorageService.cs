using System.Collections.Concurrent;
using StayHub.Application.Abstractions.Storage;

namespace StayHub.Application.IntegrationTests.Integration;

public sealed record UploadedFile(string FileName, string ContentType, string Key);

/// <summary>
/// Test double for the true external boundary (S3-compatible object storage).
/// Real success path by default; set FailNextUpload/FailNextDelete to force
/// the failure branch of handlers that compensate on storage errors.
/// </summary>
public sealed class TestFileStorageService : IFileStorageService
{
    public const string BaseUrl = "https://test-storage.local";

    private readonly ConcurrentBag<string> _deletedKeys = new();
    private readonly ConcurrentBag<UploadedFile> _uploadedFiles = new();

    public Exception? FailNextUpload { get; set; }
    public Exception? FailNextDelete { get; set; }

    public IReadOnlyCollection<UploadedFile> UploadedFiles => _uploadedFiles.ToArray();
    public IReadOnlyCollection<string> DeletedUrls => _deletedKeys.ToArray();

    public Task<string> UploadAsync(
        Stream fileContent,
        string fileName,
        string contentType,
        ImageCategory imageCategory,
        CancellationToken cancellationToken = default)
    {
        if (FailNextUpload is { } exception)
        {
            FailNextUpload = null;
            throw exception;
        }

        // Return a realistic key prefix matching ObjectStorageService behavior
        var categoryPrefix = KeyPrefix(imageCategory);
        var key = $"{categoryPrefix}/{Guid.NewGuid():N}_{fileName}";

        _uploadedFiles.Add(new UploadedFile(fileName, contentType, key));

        return Task.FromResult(key);
    }

    public Task<string> GeneratePresignedUrlAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult(string.Empty);
        }

        // Backward compatibility: If an old test saved a full URL into DB, return it as-is
        if (key.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            key.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(key);
        }

        // Standard behavior: Prepend deterministic base URL to key
        var presignedUrl = $"{BaseUrl}/{key.TrimStart('/')}";

        return Task.FromResult(presignedUrl);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        if (FailNextDelete is { } exception)
        {
            FailNextDelete = null;
            throw exception;
        }

        _deletedKeys.Add(key);

        return Task.CompletedTask;
    }

    private static string KeyPrefix(ImageCategory category)
    {
        return category switch
        {
            ImageCategory.ApartmentPhoto => "apartments",
            ImageCategory.UserAvatar => "avatars",
            _ => "misc"
        };
    }
}
using System.Collections.Concurrent;
using StayHub.Application.Abstractions.Storage;

namespace StayHub.Api.FunctionalTests.Infrastructure;

public sealed record UploadedFile(string FileName, string ContentType, string Url);

/// <summary>
/// Test double for the true external boundary (S3-compatible object storage).
/// Real success path by default; set FailNextUpload/FailNextDelete to force
/// the failure branch of handlers that compensate for storage errors.
/// </summary>
public sealed class TestFileStorageService : IFileStorageService
{
    private readonly ConcurrentBag<string> _deletedUrls = new();
    private readonly ConcurrentBag<UploadedFile> _uploadedFiles = new();

    public Exception? FailNextUpload { get; set; }
    public Exception? FailNextDelete { get; set; }

    public IReadOnlyCollection<UploadedFile> UploadedFiles => _uploadedFiles.ToArray();
    public IReadOnlyCollection<string> DeletedUrls => _deletedUrls.ToArray();

    public Task<string> UploadAsync(
        Stream fileContent,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (FailNextUpload is { } exception)
        {
            FailNextUpload = null;
            throw exception;
        }

        var url = $"https://test-storage.local/{Guid.NewGuid():N}/{fileName}";

        _uploadedFiles.Add(new UploadedFile(fileName, contentType, url));

        return Task.FromResult(url);
    }

    public Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        if (FailNextDelete is { } exception)
        {
            FailNextDelete = null;
            throw exception;
        }

        _deletedUrls.Add(url);

        return Task.CompletedTask;
    }
}
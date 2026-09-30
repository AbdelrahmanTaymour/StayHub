namespace StayHub.Application.Abstractions.Storage;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        ImageCategory category,
        CancellationToken cancellationToken = default);

    Task<string> GeneratePresignedUrlAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
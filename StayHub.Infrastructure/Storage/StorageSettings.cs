namespace StayHub.Infrastructure.Storage;

public sealed class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Informational label only - does not drive any branching logic. See ObjectStorageClientFactory.
    /// </summary>
    public StorageProvider Provider { get; init; }

    public string AccessKey { get; init; }

    public string SecretKey { get; init; }

    public string BucketName { get; init; }

    /// <summary>
    /// The base URL used to build public links to uploaded objects, e.g.
    /// "https://f005.backblazeb2.com/file/your-bucket" (Backblaze B2),
    /// "https://pub-xxxx.r2.dev" (Cloudflare R2 dev URL),
    /// or "https://bucket.s3.region.amazonaws.com" (AWS S3).
    /// </summary>
    public string PublicBaseUrl { get; init; }

    /// <summary>
    /// AWS region - only used when ServiceUrl is not set (i.e. the provider is real AWS S3).
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Explicit S3-compatible endpoint - required for any non-AWS provider (Backblaze B2, Cloudflare R2,
    /// MinIO, etc). Its presence is what tells ObjectStorageClientFactory to use a custom endpoint
    /// instead of a real AWS region.
    /// Examples: "https://s3.us-east-005.backblazeb2.com" (B2), "https://{accountId}.r2.cloudflarestorage.com" (R2).
    /// </summary>
    public string? ServiceUrl { get; init; }
}
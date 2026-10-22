namespace StayHub.Infrastructure.Storage;

public sealed class StorageSettings
{
    public const string SectionName = "Storage";

    public StorageProvider Provider { get; init; }

    public string AccessKey { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public string BucketName { get; init; } = string.Empty;

    public string? Region { get; init; } = string.Empty;

    public string? ServiceUrl { get; init; }

    public int PresignedUrlExpirationMinutes { get; init; } = 60;
}
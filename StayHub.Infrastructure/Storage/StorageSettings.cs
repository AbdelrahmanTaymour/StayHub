namespace StayHub.Infrastructure.Storage;

public sealed class StorageSettings
{
    public const string SectionName = "Storage";

    public StorageProvider Provider { get; init; }

    public string AccessKey { get; init; }

    public string SecretKey { get; init; }

    public string BucketName { get; init; }

    public string? Region { get; init; }

    public string? ServiceUrl { get; init; }

    public int PresignedUrlExpirationMinutes { get; init; } = 60;
}
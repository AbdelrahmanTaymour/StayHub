using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;

namespace StayHub.Infrastructure.Storage;

/// <summary>
/// Builds the S3-protocol client used to talk to whichever object storage provider is configured.
/// 
/// The switch is deliberately based on ServiceUrl presence, not on StorageProvider - any S3-compatible
/// host (B2, R2, MinIO, DigitalOcean Spaces, Wasabi...) just needs its endpoint set once here, with no
/// new branch required per provider. StorageProvider still exists on StorageSettings purely as a label
/// for logging/debugging ("what am I actually configured against"), not as branching logic.
/// This is the only file in the project that should reference these "Amazon"-named types directly.
/// </summary>
internal static class ObjectStorageClientFactory
{
    public static IAmazonS3 Create(IOptions<StorageSettings> storageSettings)
    {
        var settings = storageSettings.Value;
        var credentials = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);

        var config = new AmazonS3Config();

        if (!string.IsNullOrWhiteSpace(settings.ServiceUrl))
        {
            // Any non-AWS S3-compatible provider (B2, R2, MinIO...) needs an explicit endpoint
            // and path-style addressing instead of AWS's virtual-hosted-style URLs.
            config.ServiceURL = settings.ServiceUrl;
            config.ForcePathStyle = true;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region);
        }

        return new AmazonS3Client(credentials, config);
    }
}
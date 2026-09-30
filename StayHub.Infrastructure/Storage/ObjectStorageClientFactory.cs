using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;

namespace StayHub.Infrastructure.Storage;

internal static class ObjectStorageClientFactory
{
    public static IAmazonS3 Create(IOptions<StorageSettings> storageSettings)
    {
        var settings = storageSettings.Value;
        var credentials = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);

        var config = new AmazonS3Config();

        if (!string.IsNullOrWhiteSpace(settings.ServiceUrl))
        {
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
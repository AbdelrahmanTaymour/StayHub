using Hangfire;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.RemoveApartmentImage;

public sealed class DeleteApartmentImageBlobJob(IFileStorageService fileStorageService)
{
    [AutomaticRetry(Attempts = 5)]
    public async Task ExecuteAsync(ApartmentImageKey key, CancellationToken cancellationToken = default)
    {
        await fileStorageService.DeleteAsync(key.Value, cancellationToken);
    }
}
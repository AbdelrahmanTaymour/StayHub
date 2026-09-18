namespace StayHub.Infrastructure.Storage;

public sealed record ProcessedImage(Stream Content, string ContentType, string FileExtension);
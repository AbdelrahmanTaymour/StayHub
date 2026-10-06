using StayHub.Domain.Shared;

namespace StayHub.Application.Shared;

public static class ValidationHelper
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public static readonly string[] AllowedImageContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public static readonly string[] AllowedImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];

    public static bool BeValidCurrency(string code)
    {
        return Currency.All.Any(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasAllowedImageExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;

        var extension = Path.GetExtension(fileName);
        return AllowedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
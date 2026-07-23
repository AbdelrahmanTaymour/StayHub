using FluentValidation;

namespace StayHub.Application.Apartments.AddApartmentImage;

public class AddApartmentImageCommandValidator : AbstractValidator<AddApartmentImageCommand>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private static readonly string[] AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];

    public AddApartmentImageCommandValidator()
    {
        RuleFor(x => x.ApartmentId)
            .NotEmpty();

        RuleFor(x => x.RequestedByUserId)
            .NotEmpty();

        RuleFor(x => x.FileContent)
            .NotNull()
            .WithMessage("File content is required.")
            .Must(stream => stream != null && stream.Length > 0)
            .WithMessage("File cannot be empty.")
            .Must(stream => stream != null && stream.Length <= MaxFileSizeBytes)
            .WithMessage("File size must not exceed 5 MB.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .Must(HasAllowedExtension)
            .WithMessage($"File extension must be one of the following: {string.Join(", ", AllowedExtensions)}");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(contentType => AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Content type must be one of the following: {string.Join(", ", AllowedContentTypes)}");
    }

    private static bool HasAllowedExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;

        var extension = Path.GetExtension(fileName);
        return AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
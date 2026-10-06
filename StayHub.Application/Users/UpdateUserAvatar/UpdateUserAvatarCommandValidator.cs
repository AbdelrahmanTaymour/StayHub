using FluentValidation;
using StayHub.Application.Shared;

namespace StayHub.Application.Users.UpdateUserAvatar;

internal sealed class UpdateUserAvatarCommandValidator : AbstractValidator<UpdateUserAvatarCommand>
{
    public UpdateUserAvatarCommandValidator()
    {
        RuleFor(x => x.FileContent)
            .NotNull()
            .WithMessage("File content is required.")
            .Must(stream => stream != null && stream.Length > 0)
            .WithMessage("File cannot be empty.")
            .Must(stream => stream != null && stream.Length <= ValidationHelper.MaxFileSizeBytes)
            .WithMessage("File size must not exceed 5 MB.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .Must(ValidationHelper.HasAllowedImageExtension)
            .WithMessage(
                $"File extension must be one of the following: {string.Join(", ", ValidationHelper.AllowedImageExtensions)}");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(contentType =>
                ValidationHelper.AllowedImageContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            .WithMessage(
                $"Content type must be one of the following: {string.Join(", ", ValidationHelper.AllowedImageContentTypes)}");
    }
}
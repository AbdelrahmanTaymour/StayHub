using FluentValidation;

namespace StayHub.Application.Apartments.ReorderApartmentImages;

public class ReorderApartmentImagesCommandValidator : AbstractValidator<ReorderApartmentImagesCommand>
{
    public ReorderApartmentImagesCommandValidator()
    {
        RuleFor(x => x.ApartmentId)
            .NotEmpty();

        RuleFor(x => x.RequestedByUserId)
            .NotEmpty();

        RuleFor(x => x.OrderedImageIds)
            .NotNull()
            .WithMessage("Image IDs list cannot be null.")
            .Must(ids => ids != null && ids.Count > 0)
            .WithMessage("At least one image ID must be provided.")
            .Must(ContainNoEmptyGuids)
            .WithMessage("Image IDs list contains empty GUIDs.")
            .Must(ContainUniqueIds)
            .WithMessage("Duplicate image IDs are not allowed in the reorder list.");
    }

    private static bool ContainNoEmptyGuids(IReadOnlyList<Guid> ids)
    {
        return ids.All(id => id != Guid.Empty);
    }

    private static bool ContainUniqueIds(IReadOnlyList<Guid> ids)
    {
        return ids.Distinct().Count() == ids.Count;
    }
}
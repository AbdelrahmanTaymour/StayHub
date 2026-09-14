using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Email;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.InviteUser;

internal sealed class InviteUserCommandHandler(
    IApartmentRepository apartmentRepository,
    IUserContext userContext,
    IEmailService emailService) : ICommandHandler<InviteUserCommand>
{
    private const string InvitationSubject = "You've been invited to join StayHub";

    public async Task<Result> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        if (!userContext.IsOwner(apartment.OwnerId) && !userContext.IsAdmin)
            return Result.Failure(ApartmentErrors.NotAuthorized);

        var emailResult = Email.Create(request.Email);

        if (emailResult.IsFailure) return Result.Failure(emailResult.Error);

        await emailService.SendAsync(emailResult.Value, InvitationSubject, request.Body);

        return Result.Success();
    }
}
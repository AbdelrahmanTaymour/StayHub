using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Users.LogOutUser;

internal sealed class LogOutUserCommandHandler(IJwtService jwtService) : ICommandHandler<LogOutUserCommand>
{
    public Task<Result> Handle(LogOutUserCommand request, CancellationToken cancellationToken)
    {
        return jwtService.LogOutAsync(request.RefreshToken, cancellationToken);
    }
}
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Users.RefreshAccessToken;

internal sealed class RefreshAccessToken(IJwtService jwtService)
    : ICommandHandler<RefreshAccessTokenCommand, AccessTokenResponse>
{
    public Task<Result<AccessTokenResponse>> Handle(RefreshAccessTokenCommand request,
        CancellationToken cancellationToken)
    {
        return jwtService.RefreshAccessTokenAsync(request.RefreshToken, cancellationToken);
    }
}
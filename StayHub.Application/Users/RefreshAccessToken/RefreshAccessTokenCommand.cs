using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.RefreshAccessToken;

public sealed record RefreshAccessTokenCommand(string RefreshToken) : ICommand<AccessTokenResponse>;
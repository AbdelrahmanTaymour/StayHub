using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
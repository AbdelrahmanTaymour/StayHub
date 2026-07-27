using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Payments.MarkPaymentSucceeded;

public sealed record MarkPaymentSucceededCommand(string ProviderReference) : ICommand;
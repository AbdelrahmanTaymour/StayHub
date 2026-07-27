using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Payments.MarkPaymentFailed;

public sealed record MarkPaymentFailedCommand(string ProviderReference) : ICommand;
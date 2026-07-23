using MediatR;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
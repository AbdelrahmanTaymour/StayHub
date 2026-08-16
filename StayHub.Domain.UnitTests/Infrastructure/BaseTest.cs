using FluentAssertions;
using StayHub.Domain.Abstractions;

namespace StayHub.Domain.UnitTests.Infrastructure;

public abstract class BaseTest
{
    internal static T AssertDomainEventWasPublished<T>(Entity entity)
        where T : IDomainEvent
    {
        var domainEvent = entity.GetDomainEvents().OfType<T>().SingleOrDefault();

        if (domainEvent == null)
        {
            throw new Exception($"{typeof(T).Name} was not published");
        }

        return domainEvent;
    }

    internal static void AssertDomainEventWasNotPublished<T>(Entity entity)
        where T : IDomainEvent
    {
        entity.GetDomainEvents().OfType<T>().Should().BeEmpty();
    }


    internal static void AssertDomainEventWasPublishedTimes<T>(Entity entity, int expectedCount)
        where T : IDomainEvent
    {
        entity.GetDomainEvents().OfType<T>().Should().HaveCount(expectedCount);
    }
}
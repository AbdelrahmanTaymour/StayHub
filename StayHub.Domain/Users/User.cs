using StayHub.Domain.Abstractions;
using StayHub.Domain.Users.Events;

namespace StayHub.Domain.Users;

public sealed class User : Entity
{
    private User(
        Guid id,
        FirstName firstName,
        LastName lastName,
        Email email,
        DateTime createdOnUtc) : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        CreatedOnUtc = createdOnUtc;
    }

    public FirstName FirstName { get; private set; }
    public LastName LastName { get; private set; }
    public Email Email { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }


    public static User Create(Guid id, FirstName firstName, LastName lastName, Email email)
    {
        var user = new User(Guid.NewGuid(), firstName, lastName, email, DateTime.UtcNow);

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }

    public void UpdateName(FirstName firstName, LastName lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
}
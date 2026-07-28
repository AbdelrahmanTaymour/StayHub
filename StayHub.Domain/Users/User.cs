using StayHub.Domain.Abstractions;
using StayHub.Domain.Users.Events;

namespace StayHub.Domain.Users;

public sealed class User : Entity
{
    private User(Guid id, FirstName firstName, LastName lastName, Email email, DateTime createdOnUtc) : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        CreatedOnUtc = createdOnUtc;
    }

    private User()
    {
    }

    public string IdentityId { get; private set; } = string.Empty;

    public FirstName FirstName { get; private set; }

    public LastName LastName { get; private set; }

    public Email Email { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public static User Create(FirstName firstName, LastName lastName, Email email, DateTime utcNow)
    {
        var user = new User(Guid.CreateVersion7(), firstName, lastName, email, utcNow);

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }

    public void UpdateName(FirstName firstName, LastName lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public void SetIdentityId(string identityId)
    {
        IdentityId = identityId;
    }
}
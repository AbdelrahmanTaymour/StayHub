namespace StayHub.Domain.Users;

public sealed class Role
{
    public static readonly Role Guest = new(1, "Guest");
    public static readonly Role Admin = new(2, "Admin");

    public Role(int id, string name)
    {
        Id = id;
        Name = name;
    }

    private Role()
    {
    }

    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;

    public ICollection<User> Users { get; init; } = new List<User>();
    public ICollection<Permission> Permissions { get; init; } = new List<Permission>();
}
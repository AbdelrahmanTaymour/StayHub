namespace StayHub.Domain.Users;

public sealed class Permission
{
    public static readonly Permission UserRead = new(1, "user:read");
    public static readonly Permission UserUpdate = new(2, "user:update");
    public static readonly Permission UserManageSessions = new(3, "user:manage-sessions");
    public static readonly Permission ApartmentCreate = new(4, "apartment:create");
    public static readonly Permission ApartmentManage = new(5, "apartment:manage");
    public static readonly Permission BookingCreate = new(6, "booking:create");
    public static readonly Permission BookingManage = new(7, "booking:manage");
    public static readonly Permission PaymentCreate = new(8, "payment:create");
    public static readonly Permission PaymentRefund = new(9, "payment:refund");
    public static readonly Permission ReviewCreate = new(10, "review:create");
    public static readonly Permission ReviewRespond = new(11, "review:respond");
    public static readonly Permission FavoriteManage = new(12, "favorite:manage");
    public static readonly Permission ConversationManage = new(13, "conversation:manage");
    public static readonly Permission NotificationManage = new(14, "notification:manage");

    public Permission(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; init; }
    public string Name { get; init; }
}
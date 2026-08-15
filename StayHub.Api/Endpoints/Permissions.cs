namespace StayHub.Api.Endpoints;

public static class Permissions
{
    public const string UserRead = "user:read";
    public const string UserUpdate = "user:update";
    public const string UserManageSessions = "user:manage-sessions";
    public const string ApartmentCreate = "apartment:create";
    public const string ApartmentManage = "apartment:manage";
    public const string BookingCreate = "booking:create";
    public const string BookingManage = "booking:manage";
    public const string PaymentCreate = "payment:create";
    public const string PaymentRefund = "payment:refund";
    public const string ReviewCreate = "review:create";
    public const string ReviewRespond = "review:respond";
    public const string FavoriteManage = "favorite:manage";
    public const string ConversationManage = "conversation:manage";
    public const string NotificationManage = "notification:manage";
    public const string MaintenanceCreate = "maintenance:create";
    public const string MaintenanceManage = "maintenance:manage";
}
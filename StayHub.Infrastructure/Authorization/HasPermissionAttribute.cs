using Microsoft.AspNetCore.Authorization;

namespace StayHub.Infrastructure.Authorization;

public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(permission);
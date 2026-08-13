using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Role = StayHub.Domain.Users.Role;

namespace StayHub.Infrastructure.BackgroundJobs;

/// <summary>
/// Restricts /hangfire to authenticated requests carrying an admin role.
/// Adjust the role/permission check below to match your actual RBAC
/// constants (Roles.Admin / Permissions.* in Api.Controllers) — placeholder
/// name used here since I don't have the exact constant.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        HttpContext httpContext = context.GetHttpContext();

        return httpContext.User.Identity?.IsAuthenticated == true
               && httpContext.User.IsInRole(Role.Admin.Name);
    }
}
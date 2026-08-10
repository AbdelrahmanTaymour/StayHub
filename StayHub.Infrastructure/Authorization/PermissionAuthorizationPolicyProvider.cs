using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace StayHub.Infrastructure.Authorization;

internal sealed class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);

        if (policy is not null) return policy;


        // Deliberately NOT cached back into AuthorizationOptions here - AuthorizationOptions'
        // internal policy map is a plain, non-thread-safe Dictionary, and concurrent requests
        // hitting a permission for the first time simultaneously could race on writing to it.
        // Building a small AuthorizationPolicy object fresh each time is cheap enough that this
        // isn't worth the concurrency risk.
        return new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace VinamilkHRM.Infrastructure.Security;

/// <summary>
/// Nhà cung cấp chính sách phân quyền động (Dynamic Policy Provider).
/// Tự động sinh AuthorizationPolicy theo tên quyền "Module.Action" khi gặp attribute [Authorize(Policy = "Module.Action")].
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Nhận diện policy quyền hạn nếu có định dạng Module.Action (ví dụ: LUONG.Approve, NHANSU.View)
        if (policyName.Contains('.'))
        {
            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new PermissionRequirement(policyName));
            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}

using System.Security.Claims;
using Jetar.Core.Common;
using Jetar.Core.Enums;
using Jetar.Core.Interfaces;

namespace Jetar.API.Infrastructure;

/// <summary>JWT claim'laridan joriy foydalanuvchini o'qiydi.</summary>
public class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _principal;

    public CurrentUser(IHttpContextAccessor accessor) => _principal = accessor.HttpContext?.User;

    public Guid? Id
    {
        get
        {
            var raw = _principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? _principal?.FindFirst("sub")?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Username => _principal?.FindFirst(ClaimTypes.Name)?.Value;

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated == true;

    public bool IsModerator => _principal?.IsInRole(nameof(UserRole.Moderator)) == true || IsAdmin;

    public bool IsAdmin => _principal?.IsInRole(nameof(UserRole.Admin)) == true;

    public Guid RequireId() => Id ?? throw AppException.Unauthorized();
}

using Jetar.Domain.Abstractions;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;

namespace Jetar.Domain.Specifications;

/// <summary>Admin panel: foydalanuvchilarni qidirish va filtrlash (rol, bloklangan, tasdiqlangan).</summary>
public sealed class AdminUserSearchSpec : Specification<User>
{
    public AdminUserSearchSpec(
        string? search, UserRole? role, bool? blocked, bool? verified, int page, int pageSize)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().TrimStart('@', '+').ToLowerInvariant();
            Where(u => u.Username.Contains(term)
                       || u.Phone.Contains(term)
                       || u.FirstName.ToLower().Contains(term)
                       || u.LastName.ToLower().Contains(term));
        }

        if (role.HasValue) Where(u => u.Role == role.Value);
        if (blocked.HasValue) Where(u => u.IsBlocked == blocked.Value);
        if (verified.HasValue) Where(u => u.IsVerified == verified.Value);

        ApplyNoTracking();
        AddOrderByDescending(u => u.CreatedAt);
        ApplyPaging(page, pageSize);
    }
}

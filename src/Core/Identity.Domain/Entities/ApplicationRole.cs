using Microsoft.AspNetCore.Identity;

namespace Identity.Domain.Entities;

public class ApplicationRole : IdentityRole<string>
{
    public string? Description { get; set; }
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
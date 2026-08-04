namespace Identity.Domain.Entities;

public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public virtual ApplicationRole Role { get; set; } = null!;

    public string PermissionId { get; set; } = string.Empty;
    public virtual Permission Permission { get; set; } = null!;
}
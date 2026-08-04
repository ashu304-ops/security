namespace Identity.Domain.Entities;

public class Permission
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;       // e.g., Enquiry.CRUD
    public string Module { get; set; } = string.Empty;     // e.g., Counseling
    public string? Description { get; set; }
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
namespace Identity.Contracts.DTOs;

// Staff DTOs
public record StaffResponseDto(
    string Id,
    string StaffName,
    string Email,
    string? Department,
    bool IsActive,
    DateTime CreatedAt,
    IList<string> Roles
);

public record UpdateStaffStatusRequestDto(bool IsActive);

public record AssignRoleRequestDto(string RoleName);

// Role & Permission DTOs
public record RoleResponseDto(
    string Id,
    string Name,
    string? Description,
    List<string> Permissions
);

public record CreateRoleRequestDto(
    string RoleName,
    string Description,
    List<int> PermissionIds
);

public record PermissionResponseDto(
    int Id,
    string Name,
    string Module,
    string? Description
);

public record AssignPermissionsToRoleRequestDto(
    List<int> PermissionIds
);
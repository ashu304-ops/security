namespace Identity.Contracts.DTOs;

public record LoginRequestDto(string Email, string Password);

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    string StaffName,
    string Email,
    IList<string> Roles,
    IList<string> Permissions
);

public record CreateStaffRequestDto(
    string Email,
    string Password,
    string StaffName,
    string Department,
    string Role
);

public record RefreshTokenRequestDto(string RefreshToken);
public record RevokeTokenRequestDto(string RefreshToken);

public record RegisterStaffRequestDto(
    string StaffName,
    string Email,
    string Department,
    string Role,
    string Password
);
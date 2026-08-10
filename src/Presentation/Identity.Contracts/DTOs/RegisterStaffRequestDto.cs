namespace Identity.Contracts.DTOs;

public record RegisterStaffRequestDto(
    string Username,
    string Email,
    string Password,
    string StaffName,
    string Department,
    string Role
);
namespace Identity.Contracts.DTOs;

public record LoginRequestDto(string Email, string Password);

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    string StaffName,
    string Email,
    IList<string> Roles,
    IList<string> Permissions,
    bool RequiresMfa = false
);

public record RefreshTokenRequestDto(string RefreshToken);
public record RevokeTokenRequestDto(string RefreshToken);

// Admin-Only MFA & SSO Contracts
public record EnableMfaResponseDto(string SecretKey, string QrCodeUri);
public record VerifyMfaRequestDto(string EmailOrUsername, string Code);
public record GoogleLoginRequestDto(string IdToken);
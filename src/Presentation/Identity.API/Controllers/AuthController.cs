using System.Security.Claims;
using Google.Apis.Auth;
using Identity.Application.Common.Interfaces;
using Identity.Contracts.Common;
using Identity.Contracts.DTOs;
using Identity.Domain.Entities;
using Identity.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using QRCoder;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ApplicationDbContext _db;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenGenerator jwtTokenGenerator,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _db = db;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto dto)
    {
        // Search by Username first, then fall back to Email
        var user = await _userManager.FindByNameAsync(dto.Email) 
                ?? await _userManager.FindByEmailAsync(dto.Email);

        if (user == null || !user.IsActive)
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Invalid credentials or account deactivated."));

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Invalid credentials."));

        var roles = await _userManager.GetRolesAsync(user);

        // Fetch User Permissions
        var permissions = await (from ur in _db.UserRoles
                                 where ur.UserId == user.Id
                                 join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                                 join p in _db.Permissions on rp.PermissionId equals p.Id
                                 select p.Name).Distinct().ToListAsync();

        // ---------------------------------------------------------------------
        // MFA RESTRICTION: STRICTLY FOR SUPERADMIN ACCOUNTS ONLY
        // ---------------------------------------------------------------------
        if (roles.Contains("SuperAdmin"))
        {
            // Case A: First-time SuperAdmin login (MFA NOT registered yet)
            // Grant temporary access token so frontend can navigate to QR registration screen
            if (!user.IsMfaEnabled)
            {
                var (setupToken, setupJwtId) = _jwtTokenGenerator.GenerateAccessToken(user, roles, permissions);
                var setupRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

                _db.RefreshTokens.Add(new RefreshToken
                {
                    Token = setupRefreshToken,
                    JwtId = setupJwtId,
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    IsRevoked = false
                });
                await _db.SaveChangesAsync();

                return Ok(ApiResponse<AuthResponseDto>.Ok(
                    new AuthResponseDto(
                        setupToken, 
                        setupRefreshToken, 
                        user.StaffName, 
                        user.Email ?? user.UserName!, 
                        roles.ToList(), 
                        permissions, 
                        RequiresMfa: false // <-- Set false so UI routes to admin onboarding
                    ),
                    "MFA_SETUP_REQUIRED"
                ));
            }

            // Case B: Returning SuperAdmin login -> Prompt for 6-digit TOTP code
            return Ok(ApiResponse<AuthResponseDto>.Ok(
                new AuthResponseDto(
                    Token: "", 
                    RefreshToken: "", 
                    user.StaffName, 
                    user.Email ?? user.UserName!, 
                    roles.ToList(), 
                    new List<string>(), 
                    RequiresMfa: true // <-- Set true to show 6-digit input UI
                ),
                "MFA_VERIFICATION_REQUIRED"
            ));
        }

        // ---------------------------------------------------------------------
        // REGULAR STAFF / COUNSELLOR LOGIN (NO MFA REQUIRED)
        // ---------------------------------------------------------------------
        var (token, jwtId) = _jwtTokenGenerator.GenerateAccessToken(user, roles, permissions);
        var refreshTokenStr = _jwtTokenGenerator.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshTokenStr,
            JwtId = jwtId,
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        });

        await _db.SaveChangesAsync();

        var response = new AuthResponseDto(
            token,
            refreshTokenStr,
            user.StaffName,
            user.Email ?? user.UserName!,
            roles.ToList(),
            permissions,
            RequiresMfa: false
        );

        return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Login successful."));
    }

    [HttpPost("enable-mfa")]
    [AllowAnonymous] // Allows generating onboarding QR code during setup
    public async Task<ActionResult<ApiResponse<EnableMfaResponseDto>>> EnableMfa([FromBody] MfaSetupRequestDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.EmailOrUsername) 
                ?? await _userManager.FindByEmailAsync(dto.EmailOrUsername);

        if (user == null || !user.IsActive)
            return BadRequest(ApiResponse<EnableMfaResponseDto>.Fail("User account not found or inactive."));

        // Generate 20-byte random secret key
        var secretKeyBytes = KeyGeneration.GenerateRandomKey(20);
        var secretKeyBase32 = Base32Encoding.ToString(secretKeyBytes);

        // Persist the generated key immediately
        user.MfaSecretKey = secretKeyBase32;
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
            return BadRequest(ApiResponse<EnableMfaResponseDto>.Fail("Failed to save MFA secret key to user account."));

        // Build TOTP URI for Authenticator App
        var otpUri = $"otpauth://totp/ComputerSeekhoAdmin:{user.Email}?secret={secretKeyBase32}&issuer=ComputerSeekho";

        // Generate QR Code PNG Data URI
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(otpUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeGraphic = qrCode.GetGraphic(20);
        var qrCodeBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrCodeGraphic)}";

        return Ok(ApiResponse<EnableMfaResponseDto>.Ok(
            new EnableMfaResponseDto(secretKeyBase32, qrCodeBase64),
            "Admin MFA secret generated successfully. Scan QR code in Authenticator app."
        ));
    }

    [HttpPost("verify-mfa")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> VerifyMfa([FromBody] VerifyMfaRequestDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.EmailOrUsername) 
                ?? await _userManager.FindByEmailAsync(dto.EmailOrUsername);

        if (user == null || !user.IsActive || string.IsNullOrEmpty(user.MfaSecretKey))
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Invalid request or MFA not configured."));

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains("SuperAdmin"))
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("MFA verification is restricted to SuperAdmin accounts."));

        var secretBytes = Base32Encoding.ToBytes(user.MfaSecretKey);
        var totp = new Totp(secretBytes);

        // Allow a +/- 60 seconds time window tolerance for clock drift
        var window = new VerificationWindow(previous: 2, future: 2);
        bool isValid = totp.VerifyTotp(dto.Code, out _, window);

        if (!isValid)
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Invalid or expired 6-digit MFA code."));

        // Permanently enable MFA for user upon successful verification
        user.IsMfaEnabled = true;
        await _userManager.UpdateAsync(user);

        var permissions = await (from ur in _db.UserRoles
                                 where ur.UserId == user.Id
                                 join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                                 join p in _db.Permissions on rp.PermissionId equals p.Id
                                 select p.Name).Distinct().ToListAsync();

        var (token, jwtId) = _jwtTokenGenerator.GenerateAccessToken(user, roles, permissions);
        var refreshTokenStr = _jwtTokenGenerator.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshTokenStr,
            JwtId = jwtId,
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        });

        await _db.SaveChangesAsync();

        var response = new AuthResponseDto(
            token,
            refreshTokenStr,
            user.StaffName,
            user.Email ?? user.UserName!,
            roles.ToList(),
            permissions,
            RequiresMfa: false
        );

        return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Admin MFA verification successful."));
    }

    [HttpPost("google-admin-login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> GoogleAdminLogin([FromBody] GoogleLoginRequestDto dto)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);

            var user = await _userManager.FindByEmailAsync(payload.Email);
            if (user == null)
                return BadRequest(ApiResponse<AuthResponseDto>.Fail("Google account is not registered as an Admin."));

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("SuperAdmin"))
                return BadRequest(ApiResponse<AuthResponseDto>.Fail("Access Denied: Google SSO is restricted to SuperAdmins only."));

            var permissions = await (from ur in _db.UserRoles
                                     where ur.UserId == user.Id
                                     join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                                     join p in _db.Permissions on rp.PermissionId equals p.Id
                                     select p.Name).Distinct().ToListAsync();

            var (token, jwtId) = _jwtTokenGenerator.GenerateAccessToken(user, roles, permissions);
            var refreshTokenStr = _jwtTokenGenerator.GenerateRefreshToken();

            _db.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshTokenStr,
                JwtId = jwtId,
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            });

            await _db.SaveChangesAsync();

            var response = new AuthResponseDto(
                token,
                refreshTokenStr,
                user.StaffName,
                user.Email ?? user.UserName!,
                roles.ToList(),
                permissions,
                RequiresMfa: false
            );

            return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Admin Google SSO successful."));
        }
        catch (InvalidJwtException)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Invalid or expired Google token."));
        }
    }

    [HttpPost("register-staff")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<string>>> RegisterStaff([FromBody] RegisterStaffRequestDto dto)
    {
        var username = string.IsNullOrWhiteSpace(dto.Username) ? dto.Email : dto.Username;

        var existingUserByName = await _userManager.FindByNameAsync(username);
        if (existingUserByName != null)
            return BadRequest(ApiResponse<string>.Fail("A user with this username already exists."));

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var existingUserByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUserByEmail != null)
                return BadRequest(ApiResponse<string>.Fail("A user with this email already exists."));
        }

        if (!await _roleManager.RoleExistsAsync(dto.Role))
            return BadRequest(ApiResponse<string>.Fail($"Role '{dto.Role}' does not exist in the system."));

        var user = new ApplicationUser
        {
            UserName = username,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? $"{username}@computerseekho.com" : dto.Email,
            StaffName = dto.StaffName,
            Department = dto.Department,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(ApiResponse<string>.Fail(errors));
        }

        await _userManager.AddToRoleAsync(user, dto.Role);

        return Ok(ApiResponse<string>.Ok(user.Id, $"Staff member '{username}' registered successfully."));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiryDate <= DateTime.UtcNow)
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Invalid or expired refresh token."));

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user == null || !user.IsActive)
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("User account is inactive or missing."));

        storedToken.IsRevoked = true;
        _db.RefreshTokens.Update(storedToken);

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await (from ur in _db.UserRoles
                                 where ur.UserId == user.Id
                                 join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                                 join p in _db.Permissions on rp.PermissionId equals p.Id
                                 select p.Name).Distinct().ToListAsync();

        var (newToken, newJwtId) = _jwtTokenGenerator.GenerateAccessToken(user, roles, permissions);
        var newRefreshTokenStr = _jwtTokenGenerator.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshTokenStr,
            JwtId = newJwtId,
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        });

        await _db.SaveChangesAsync();

        var response = new AuthResponseDto(
            newToken,
            newRefreshTokenStr,
            user.StaffName,
            user.Email ?? user.UserName!,
            roles.ToList(),
            permissions,
            RequiresMfa: false
        );

        return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Token refreshed successfully."));
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<string>>> RevokeToken([FromBody] RevokeTokenRequestDto dto)
    {
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

        if (storedToken == null)
            return NotFound(ApiResponse<string>.Fail("Refresh token not found."));

        storedToken.IsRevoked = true;
        _db.RefreshTokens.Update(storedToken);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok(storedToken.Token, "Refresh token revoked successfully."));
    }
}

public record MfaSetupRequestDto(string EmailOrUsername);
using System.Security.Claims;
using Identity.Application.Common.Interfaces;
using Identity.Contracts.Common;
using Identity.Contracts.DTOs;
using Identity.Domain.Entities;
using Identity.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Invalid credentials or account deactivated."));

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Invalid credentials."));

        var roles = await _userManager.GetRolesAsync(user);

        var permissions = await (from ur in _db.UserRoles
                                 where ur.UserId == user.Id
                                 join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                                 join p in _db.Permissions on rp.PermissionId equals p.Id
                                 select p.Name).Distinct().ToListAsync();

        var (token, jwtId) = _jwtTokenGenerator.GenerateAccessToken(user, roles, permissions);
        var refreshTokenStr = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshTokenStr,
            JwtId = jwtId,
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync();

        var response = new AuthResponseDto(
            token,
            refreshTokenStr,
            user.StaffName,
            user.Email!,
            roles.ToList(),
            permissions
        );

        return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Login successful."));
    }

    [HttpPost("register-staff")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<string>>> RegisterStaff([FromBody] RegisterStaffRequestDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return BadRequest(ApiResponse<string>.Fail("A user with this email already exists."));

        if (!await _roleManager.RoleExistsAsync(dto.Role))
            return BadRequest(ApiResponse<string>.Fail($"Role '{dto.Role}' does not exist in the system."));

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
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

        return Ok(ApiResponse<string>.Ok(user.Id, "Staff member registered successfully."));
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
            user.Email!,
            roles.ToList(),
            permissions
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
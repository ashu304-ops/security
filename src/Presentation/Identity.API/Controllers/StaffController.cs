using Identity.Contracts.Common;
using Identity.Contracts.DTOs;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Controllers;

// Request DTO for flexible single or multi-role updates
public record UpdateRolesRequestDto(List<string>? Roles, string? RoleName);

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class StaffController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public StaffController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<StaffResponseDto>>>> GetAllStaff()
    {
        var users = await _userManager.Users.ToListAsync();
        var resultList = new List<StaffResponseDto>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            resultList.Add(new StaffResponseDto(
                u.Id,
                u.StaffName,
                u.Email!,
                u.Department,
                u.IsActive,
                u.CreatedAt,
                roles
            ));
        }

        return Ok(ApiResponse<List<StaffResponseDto>>.Ok(resultList, "Staff list retrieved successfully."));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<StaffResponseDto>>> GetStaffById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<StaffResponseDto>.Fail("Staff user not found."));

        var roles = await _userManager.GetRolesAsync(user);
        var response = new StaffResponseDto(
            user.Id,
            user.StaffName,
            user.Email!,
            user.Department,
            user.IsActive,
            user.CreatedAt,
            roles
        );

        return Ok(ApiResponse<StaffResponseDto>.Ok(response));
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateStaffStatus(string id, [FromBody] UpdateStaffStatusRequestDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<string>.Fail("Staff user not found."));

        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);
        return Ok(ApiResponse<string>.Ok(user.Id, $"Staff active status updated to {dto.IsActive}."));
    }

    /// <summary>
    /// Replaces user roles with single or multiple assigned roles
    /// Route: POST /api/Staff/{id}/assign-role or PUT /api/Staff/{id}/roles
    /// </summary>
    [HttpPost("{id}/assign-role")]
    [HttpPut("{id}/roles")]
    public async Task<ActionResult<ApiResponse<string>>> AssignRoles(string id, [FromBody] UpdateRolesRequestDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<string>.Fail("Staff user not found."));

        // Build list of target roles from payload
        var rolesToAssign = dto.Roles ?? new List<string>();
        if (!string.IsNullOrWhiteSpace(dto.RoleName) && !rolesToAssign.Contains(dto.RoleName))
        {
            rolesToAssign.Add(dto.RoleName);
        }

        if (!rolesToAssign.Any())
        {
            return BadRequest(ApiResponse<string>.Fail("User must be assigned at least one role."));
        }

        // Validate that requested roles exist in database
        foreach (var roleName in rolesToAssign)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return BadRequest(ApiResponse<string>.Fail($"Role '{roleName}' does not exist."));
            }
        }

        // Remove old roles and assign new roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        var result = await _userManager.AddToRolesAsync(user, rolesToAssign);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(ApiResponse<string>.Fail(errors));
        }

        return Ok(ApiResponse<string>.Ok(user.Id, $"Roles updated to: {string.Join(", ", rolesToAssign)}"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteStaff(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<string>.Fail("Staff user not found."));

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(ApiResponse<string>.Fail(errors));
        }

        return Ok(ApiResponse<string>.Ok(id, "Staff user deleted successfully."));
    }
}
using Identity.Contracts.Common;
using Identity.Contracts.DTOs;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class StaffController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public StaffController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
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

    [HttpPost("{id}/assign-role")]
    public async Task<ActionResult<ApiResponse<string>>> AssignRole(string id, [FromBody] AssignRoleRequestDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<string>.Fail("Staff user not found."));

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        var result = await _userManager.AddToRoleAsync(user, dto.RoleName);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<string>.Fail(string.Join(", ", result.Errors.Select(e => e.Description))));

        return Ok(ApiResponse<string>.Ok(user.Id, $"Role '{dto.RoleName}' assigned successfully."));
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
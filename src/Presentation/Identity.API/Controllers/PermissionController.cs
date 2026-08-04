using Identity.Contracts.Common;
using Identity.Contracts.DTOs;
using Identity.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class PermissionController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PermissionController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PermissionResponseDto>>>> GetAllPermissions()
    {
        var rawPermissions = await _db.Permissions.ToListAsync();

        var permissions = rawPermissions
            .Select(p => new PermissionResponseDto(
                int.TryParse(p.Id, out var parsedId) ? parsedId : 0, 
                p.Name, 
                p.Module, 
                p.Description
            ))
            .ToList();

        return Ok(ApiResponse<List<PermissionResponseDto>>.Ok(permissions));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PermissionResponseDto>>> GetPermissionById(string id)
    {
        var permission = await _db.Permissions.FindAsync(id);
        if (permission == null)
            return NotFound(ApiResponse<PermissionResponseDto>.Fail("Permission not found."));

        var response = new PermissionResponseDto(
            int.TryParse(permission.Id, out var parsedId) ? parsedId : 0,
            permission.Name,
            permission.Module,
            permission.Description
        );

        return Ok(ApiResponse<PermissionResponseDto>.Ok(response));
    }
}
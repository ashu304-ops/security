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
[Authorize(Roles = "SuperAdmin")]
public class RoleController : ControllerBase
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public RoleController(RoleManager<ApplicationRole> roleManager, ApplicationDbContext db)
    {
        _roleManager = roleManager;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleResponseDto>>>> GetRoles()
    {
        try
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var allRolePermissions = await _db.RolePermissions.ToListAsync();
            var allPermissions = await _db.Permissions.ToListAsync();

            var resultList = new List<RoleResponseDto>();

            foreach (var role in roles)
            {
                // Get permission IDs linked to this role (comparing as string to prevent type mismatch)
                var assignedPermissionIds = allRolePermissions
                    .Where(rp => rp.RoleId == role.Id)
                    .Select(rp => rp.PermissionId.ToString())
                    .ToHashSet();

                // Match with Permission entities
                var permissionNames = allPermissions
                    .Where(p => assignedPermissionIds.Contains(p.Id.ToString()))
                    .Select(p => p.Name)
                    .ToList();

                resultList.Add(new RoleResponseDto(
                    role.Id, 
                    role.Name!, 
                    role.Description ?? "", 
                    permissionNames
                ));
            }

            return Ok(ApiResponse<List<RoleResponseDto>>.Ok(resultList));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<RoleResponseDto>>.Fail($"GetRoles Error: {ex.InnerException?.Message ?? ex.Message}"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> CreateRole([FromBody] CreateRoleRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.RoleName))
                return BadRequest(ApiResponse<string>.Fail("Role name is required."));

            var roleExists = await _roleManager.RoleExistsAsync(dto.RoleName);
            if (roleExists)
                return BadRequest(ApiResponse<string>.Fail($"Role '{dto.RoleName}' already exists."));

            // Explicitly set Id in memory to resolve EF Core null key tracking without DB changes
            var role = new ApplicationRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = dto.RoleName,
                NormalizedName = dto.RoleName.ToUpperInvariant(),
                Description = dto.Description
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(ApiResponse<string>.Fail(errors));
            }

            // Assign permissions if provided
            if (dto.PermissionIds != null && dto.PermissionIds.Any())
            {
                // Load all existing permissions from DB into memory
                var dbPermissions = await _db.Permissions.ToListAsync();

                foreach (var inputPermId in dto.PermissionIds)
                {
                    string inputPermStr = inputPermId.ToString();

                    // Match input ID against DB permission IDs flexibly
                    var matchedPermission = dbPermissions
                        .FirstOrDefault(p => p.Id.ToString() == inputPermStr);

                    if (matchedPermission != null)
                    {
                        _db.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = matchedPermission.Id // Pass exact DB type value
                        });
                    }
                }

                await _db.SaveChangesAsync();
            }

            return Ok(ApiResponse<string>.Ok(role.Id, $"Role '{dto.RoleName}' created successfully."));
        }
        catch (DbUpdateException dbEx)
        {
            return StatusCode(500, ApiResponse<string>.Fail($"Database Save Error: {dbEx.InnerException?.Message ?? dbEx.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail($"Server Error: {ex.InnerException?.Message ?? ex.Message}"));
        }
    }
}
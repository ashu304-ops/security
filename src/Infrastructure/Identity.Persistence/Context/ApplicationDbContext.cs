using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Context;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Customize table mappings
        builder.Entity<ApplicationUser>(e => e.ToTable("Users"));
        builder.Entity<ApplicationRole>(e => e.ToTable("Roles"));
        builder.Entity<IdentityUserRole<string>>(e => e.ToTable("UserRoles"));
        builder.Entity<IdentityUserClaim<string>>(e => e.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<string>>(e => e.ToTable("UserLogins"));
        builder.Entity<IdentityRoleClaim<string>>(e => e.ToTable("RoleClaims"));
        builder.Entity<IdentityUserToken<string>>(e => e.ToTable("UserTokens"));

        // Composite Key for RolePermission
        builder.Entity<RolePermission>(e =>
        {
            e.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            e.HasOne(rp => rp.Role)
             .WithMany(r => r.RolePermissions)
             .HasForeignKey(rp => rp.RoleId);

            e.HasOne(rp => rp.Permission)
             .WithMany(p => p.RolePermissions)
             .HasForeignKey(rp => rp.PermissionId);
        });

        SeedBrdRolesAndPermissions(builder);
    }

    private static void SeedBrdRolesAndPermissions(ModelBuilder builder)
    {
        var superAdminRoleId = "role-super-admin-id";
        var contentManagerRoleId = "role-content-manager-id";
        var counselorRoleId = "role-counselor-id";

        // Seed Computer Seekho BRD Roles
        builder.Entity<ApplicationRole>().HasData(
            new ApplicationRole { Id = superAdminRoleId, Name = "SuperAdmin", NormalizedName = "SUPERADMIN", Description = "System Super Admin" },
            new ApplicationRole { Id = contentManagerRoleId, Name = "ContentManager", NormalizedName = "CONTENTMANAGER", Description = "Manages Courses, Batches, Faculty & Master Data" },
            new ApplicationRole { Id = counselorRoleId, Name = "Counselor", NormalizedName = "COUNSELOR", Description = "Front-Office Operations: Enquiries, Follow-ups, Registration & Fees" }
        );

        // Seed BRD Permissions
        var pCourseCrud = new Permission { Id = "perm-1", Name = "Course.CRUD", Module = "Content", Description = "Manage courses, batches, and website content" };
        var pMasterTable = new Permission { Id = "perm-2", Name = "Table.Maintenance", Module = "Content", Description = "BRD 6.4: Master table maintenance" };
        var pExcelUpload = new Permission { Id = "perm-3", Name = "Excel.Upload", Module = "Content", Description = "BRD 6.5: Bulk excel data upload" };

        var pEnquiryCrud = new Permission { Id = "perm-4", Name = "Enquiry.CRUD", Module = "Counseling", Description = "BRD 6.1: Add & manage enquiries" };
        var pFollowUpView = new Permission { Id = "perm-5", Name = "FollowUp.View", Module = "Counseling", Description = "BRD 6.2: Access follow-up dashboard" };
        var pStudentRegister = new Permission { Id = "perm-6", Name = "Student.Register", Module = "Counseling", Description = "BRD 6.3: Register students" };
        var pPaymentProcess = new Permission { Id = "perm-7", Name = "Payment.Process", Module = "Counseling", Description = "Process fee payments and generate receipts" };

        var pStaffManage = new Permission { Id = "perm-8", Name = "Staff.Manage", Module = "Admin", Description = "Manage staff users and assign roles" };

        builder.Entity<Permission>().HasData(
            pCourseCrud, pMasterTable, pExcelUpload,
            pEnquiryCrud, pFollowUpView, pStudentRegister, pPaymentProcess,
            pStaffManage
        );

        // Map Permissions to Roles
        builder.Entity<RolePermission>().HasData(
            // SuperAdmin Permissions
            new RolePermission { RoleId = superAdminRoleId, PermissionId = pStaffManage.Id },
            new RolePermission { RoleId = superAdminRoleId, PermissionId = pCourseCrud.Id },
            new RolePermission { RoleId = superAdminRoleId, PermissionId = pMasterTable.Id },
            new RolePermission { RoleId = superAdminRoleId, PermissionId = pExcelUpload.Id },
            new RolePermission { RoleId = superAdminRoleId, PermissionId = pEnquiryCrud.Id },
            new RolePermission { RoleId = superAdminRoleId, PermissionId = pFollowUpView.Id },
            new RolePermission { RoleId = superAdminRoleId, PermissionId = pStudentRegister.Id },
            new RolePermission { RoleId = superAdminRoleId, PermissionId = pPaymentProcess.Id },

            // ContentManager Permissions
            new RolePermission { RoleId = contentManagerRoleId, PermissionId = pCourseCrud.Id },
            new RolePermission { RoleId = contentManagerRoleId, PermissionId = pMasterTable.Id },
            new RolePermission { RoleId = contentManagerRoleId, PermissionId = pExcelUpload.Id },

            // Counselor Permissions
            new RolePermission { RoleId = counselorRoleId, PermissionId = pEnquiryCrud.Id },
            new RolePermission { RoleId = counselorRoleId, PermissionId = pFollowUpView.Id },
            new RolePermission { RoleId = counselorRoleId, PermissionId = pStudentRegister.Id },
            new RolePermission { RoleId = counselorRoleId, PermissionId = pPaymentProcess.Id }
        );
    }
}
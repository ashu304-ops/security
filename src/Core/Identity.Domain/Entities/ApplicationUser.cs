using Microsoft.AspNetCore.Identity;

namespace Identity.Domain.Entities;

public class ApplicationUser : IdentityUser<string>
{
    public ApplicationUser()
    {
        Id = Guid.NewGuid().ToString();
    }

    public string StaffName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Admin MFA / TOTP Fields
    public bool IsMfaEnabled { get; set; } = false;
    public string? MfaSecretKey { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
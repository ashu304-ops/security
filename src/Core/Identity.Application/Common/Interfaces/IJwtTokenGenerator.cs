using Identity.Domain.Entities;

namespace Identity.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, string JwtId) GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
}
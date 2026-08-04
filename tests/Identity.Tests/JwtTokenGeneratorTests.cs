using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Identity.Tests;

public class JwtTokenGeneratorTests
{
    private readonly JwtTokenGenerator _sut;

    public JwtTokenGeneratorTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:SecretKey", "SuperSecretKeyAtLeast256BitsLongForHmacSha256Security!"},
            {"Jwt:Issuer", "Identity.API"},
            {"Jwt:Audience", "ComputerSeekho.Client"},
            {"Jwt:ExpiryMinutes", "60"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _sut = new JwtTokenGenerator(configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwtTokenAndId()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "admin@computerseekho.com",
            StaffName = "System SuperAdmin"
        };
        var roles = new List<string> { "SuperAdmin" };
        var permissions = new List<string> { "Staff.Manage", "Course.CRUD" };

        // Act
        var (token, jwtId) = _sut.GenerateAccessToken(user, roles, permissions);

        // Assert
        token.Should().NotBeNullOrEmpty();
        jwtId.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("Identity.API");
        jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == "Permission" && c.Value == "Staff.Manage");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnRandomBase64String()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert
        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
        token1.Should().NotBe(token2);
    }
}
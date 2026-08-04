using Identity.API.Controllers;
using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using Identity.Persistence.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Identity.Tests;

public class AuthControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userStoreMock;
    private readonly Mock<RoleManager<ApplicationRole>> _roleStoreMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly ApplicationDbContext _db;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        // Mock UserManager
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userStoreMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!
        );

        // Mock RoleManager
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        _roleStoreMock = new Mock<RoleManager<ApplicationRole>>(
            roleStore.Object, null!, null!, null!, null!
        );

        // Mock SignInManager
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var userClaimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userStoreMock.Object, contextAccessor.Object, userClaimsPrincipalFactory.Object, null!, null!, null!, null!
        );

        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        // Create In-Memory DbContext directly without TestHelpers
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        // Instantiate AuthController with all 5 required dependencies
        _controller = new AuthController(
            _userStoreMock.Object,
            _roleStoreMock.Object,
            _signInManagerMock.Object,
            _jwtTokenGeneratorMock.Object,
            _db
        );
    }
}
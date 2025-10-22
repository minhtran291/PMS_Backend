using Microsoft.AspNetCore.Identity;
using Moq;
using PMS.Application.Services.Auth;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using PMS.Tests.TestBase;

[TestFixture]
public class LoginServiceTests : ServiceTestBase
{
    private LoginService _loginService;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loginService = new LoginService(
            UnitOfWorkMock.Object,
            MapperMock.Object,
            TokenServiceMock.Object,
            UserServiceMock.Object,
            LoggerMock
        );
    }

    [Test]
    public async Task Login_ShouldReturnBadRequest_WhenUserNotFound()
    {
        var request = SampleData.ValidLoginRequest;

        UserManagerMock
            .Setup(x => x.FindByEmailAsync(request.UsernameOrEmail))
            .ReturnsAsync((User)null);

        var result = await _loginService.Login(request);

        Assert.That(result.StatusCode, Is.EqualTo(400));
        Assert.That(result.Message, Does.Contain("không tồn tại"));
    }

    [Test]
    public async Task Login_ShouldReturnBadRequest_WhenEmailNotConfirmed()
    {
        var user = SampleData.ExistingUser;
        user.EmailConfirmed = false;
        var request = SampleData.ValidLoginRequest;

        UserManagerMock.Setup(x => x.FindByEmailAsync(request.UsernameOrEmail))
            .ReturnsAsync(user);

        var result = await _loginService.Login(request);

        Assert.That(result.StatusCode, Is.EqualTo(400));
        Assert.That(result.Message, Does.Contain("chưa được xác nhận"));
    }

    [Test]
    public async Task Login_ShouldReturnBadRequest_WhenAccountBlocked()
    {
        var user = SampleData.ExistingUser;
        user.EmailConfirmed = true;
        user.UserStatus = UserStatus.Block;

        var request = SampleData.ValidLoginRequest;

        UserManagerMock.Setup(x => x.FindByEmailAsync(request.UsernameOrEmail))
            .ReturnsAsync(user);

        var result = await _loginService.Login(request);

        Assert.That(result.StatusCode, Is.EqualTo(400));
        Assert.That(result.Message, Does.Contain("bị khóa"));
    }

    [Test]
    public async Task Login_ShouldReturnBadRequest_WhenWrongPassword()
    {
        var user = SampleData.ExistingUser;
        user.EmailConfirmed = true;
        user.UserStatus = UserStatus.Active;

        var request = SampleData.ValidLoginRequest;

        UserManagerMock.Setup(x => x.FindByEmailAsync(request.UsernameOrEmail))
            .ReturnsAsync(user);

        SignInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, false))
            .ReturnsAsync(SignInResult.Failed);

        var result = await _loginService.Login(request);

        Assert.That(result.StatusCode, Is.EqualTo(400));
        Assert.That(result.Message, Does.Contain("không chính xác"));
    }

    [Test]
    public async Task Login_ShouldReturnBadRequest_WhenNoRolesFound()
    {
        var user = SampleData.ExistingUser;
        var request = SampleData.ValidLoginRequest;

        UserManagerMock.Setup(x => x.FindByEmailAsync(request.UsernameOrEmail))
            .ReturnsAsync(user);

        SignInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, false))
            .ReturnsAsync(SignInResult.Success);

        UserServiceMock
            .Setup(x => x.GetUserRoles(user))
            .ReturnsAsync((IList<string>)null);

        var result = await _loginService.Login(request);

        Assert.That(result.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task Login_ShouldReturnSuccess_WhenCustomerLoginIsValid()
    {
        // Arrange
        var user = SampleData.ExistingUser;

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, SampleData.ValidLoginRequest.Password);

        var request = SampleData.ValidLoginRequest;

        UserManagerMock
            .Setup(x => x.FindByEmailAsync(request.UsernameOrEmail))
            .ReturnsAsync(user);

        SignInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, false))
            .ReturnsAsync(SignInResult.Success);

        UserServiceMock
            .Setup(x => x.GetUserRoles(user))
            .ReturnsAsync(new List<string> { "Customer" });

        UnitOfWorkMock.Setup(x => x.Users.Query())
            .Returns(new List<User> { user }.AsQueryable());

        // Act
        var result = await _loginService.Login(request);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(result.Data, Is.Not.Null);
    }

    [Test]
    public async Task Login_ShouldReturnSuccess_WhenStaffLoginIsValid()
    {
        // Arrange
        var user = SampleData.ExistingUser;

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, SampleData.ValidLoginRequest.Password);

        var request = SampleData.ValidLoginRequest;

        UserManagerMock
            .Setup(x => x.FindByEmailAsync(request.UsernameOrEmail))
            .ReturnsAsync(user);

        SignInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, false))
            .ReturnsAsync(SignInResult.Success);

        UserServiceMock
            .Setup(x => x.GetUserRoles(user))
            .ReturnsAsync(new List<string> { "Staff" });

        UnitOfWorkMock.Setup(x => x.Users.Query())
            .Returns(new List<User> { user }.AsQueryable());

        // Act
        var result = await _loginService.Login(request);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(result.Data, Is.Not.Null);
    }

    [Test]
    public async Task Login_ShouldReturn500_WhenExceptionIsThrown()
    {
        // Arrange
        var request = SampleData.ValidLoginRequest;

        UserManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ThrowsAsync(new System.Exception("Database error"));

        // Act
        var result = await _loginService.Login(request);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(500));
        Assert.That(result.Message, Is.EqualTo("Lỗi"));
    }

}

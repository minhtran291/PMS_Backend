using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PMS.API.Services.Auth;
using PMS.API.Services.User;
using PMS.Core.DTO.Auth;
using PMS.Core.Domain.Identity;
using PMS.Data.UnitOfWork;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using PMS.Core.Domain.Enums;

namespace PMS.Tests.Services.Auth
{
    public class LoginServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ILogger<LoginService>> _loggerMock;
        private readonly LoginService _loginService;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<SignInManager<User>> _signInManagerMock;

        public LoginServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _tokenServiceMock = new Mock<ITokenService>();
            _userServiceMock = new Mock<IUserService>();
            _loggerMock = new Mock<ILogger<LoginService>>();

            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            _signInManagerMock = new Mock<SignInManager<User>>(
                _userManagerMock.Object,
                Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                null, null, null, null);

            _unitOfWorkMock.Setup(u => u.Users.UserManager).Returns(_userManagerMock.Object);
            _unitOfWorkMock.Setup(u => u.Users.SignInManager).Returns(_signInManagerMock.Object);

            _loginService = new LoginService(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _tokenServiceMock.Object,
                _userServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var user = new User
            {
                Id = "1",
                UserName = "testuser",
                Email = "test@example.com",
                EmailConfirmed = true,
                UserStatus = UserStatus.Active
            };

            var request = new LoginRequest
            {
                UsernameOrEmail = "test@example.com",
                Password = "Password123"
            };

            _unitOfWorkMock.Setup(u => u.Users.UserManager.FindByEmailAsync(request.UsernameOrEmail))
                .ReturnsAsync(user);

            _unitOfWorkMock.Setup(u => u.Users.SignInManager.CheckPasswordSignInAsync(user, request.Password, false))
                .ReturnsAsync(SignInResult.Success);

            _userServiceMock.Setup(s => s.GetUserRoles(user))
                .ReturnsAsync(new List<string> { "Admin" });

            _tokenServiceMock.Setup(t => t.CreateClaimForAccessToken(user, It.IsAny<IList<string>>()))
                .Returns(new List<System.Security.Claims.Claim>());

            _tokenServiceMock.Setup(t => t.GenerateToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>(), 1))
                .Returns("access_token");

            _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
                .Returns("refresh_token");

            // Act
            var result = await _loginService.Login(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("access_token", result.AccessToken);
            Assert.Equal("refresh_token", result.RefreshToken);
            _unitOfWorkMock.Verify(u => u.Users.UserManager.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldThrowException_WhenEmailNotFound()
        {
            // Arrange
            var request = new LoginRequest
            {
                UsernameOrEmail = "notfound@example.com",
                Password = "Password123"
            };

            _unitOfWorkMock.Setup(u => u.Users.UserManager.FindByEmailAsync(request.UsernameOrEmail))
                .ReturnsAsync((User)null);

            // Act + Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _loginService.Login(request));
            Assert.Equal("Email hoặc mật khẩu không chính xác", ex.Message);
        }

        [Fact]
        public async Task Login_ShouldThrowException_WhenPasswordIncorrect()
        {
            // Arrange
            var user = new User
            {
                Id = "1",
                Email = "test@example.com",
                EmailConfirmed = true,
                UserStatus = UserStatus.Active
            };

            var request = new LoginRequest
            {
                UsernameOrEmail = "test@example.com",
                Password = "wrong"
            };

            _unitOfWorkMock.Setup(u => u.Users.UserManager.FindByEmailAsync(request.UsernameOrEmail))
                .ReturnsAsync(user);

            _unitOfWorkMock.Setup(u => u.Users.SignInManager.CheckPasswordSignInAsync(user, request.Password, false))
                .ReturnsAsync(SignInResult.Failed);

            // Act + Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _loginService.Login(request));
            Assert.Equal("Email hoặc mật khẩu không chính xác", ex.Message);
        }

        [Fact]
        public async Task Login_ShouldThrowException_WhenUserBlocked()
        {
            // Arrange
            var user = new User
            {
                Id = "1",
                Email = "test@example.com",
                EmailConfirmed = true,
                UserStatus = UserStatus.Block
            };

            var request = new LoginRequest
            {
                UsernameOrEmail = "test@example.com",
                Password = "Password123"
            };

            _unitOfWorkMock.Setup(u => u.Users.UserManager.FindByEmailAsync(request.UsernameOrEmail))
                .ReturnsAsync(user);

            // Act + Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _loginService.Login(request));
            Assert.Equal("Tài khoản của bạn đã bị khóa", ex.Message);
        }
    }
}

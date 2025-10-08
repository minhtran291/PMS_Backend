using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PMS.API.Services.ExternalService;
using PMS.API.Services.User;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Identity;
using PMS.Core.DTO.Content;
using PMS.Core.DTO.Request;
using PMS.Data.Repositories.CustomerProfile;
using PMS.Data.Repositories.Profile;
using PMS.Data.Repositories.User;
using PMS.Data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace PMS.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _emailServiceMock = new Mock<IEmailService>();
            _loggerMock = new Mock<ILogger<UserService>>();

            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

            // Setup UserManager trả về từ UnitOfWork
            _unitOfWorkMock.Setup(u => u.Users.UserManager).Returns(_userManagerMock.Object);

            // Khởi tạo service thật
            _userService = new UserService(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _emailServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldCreateUser_WhenValidData()
        {
            // Arrange
            var registerRequest = new RegisterUser
            {
                UserName = "testuser",
                Password = "Test@123",
                ConfirmPassword = "Test@123",
                Email = "test@example.com",
                PhoneNumber = "0123456789",
                Address = "123 street"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(registerRequest.Email))
                .ReturnsAsync((User)null);

            var fakeUsers = new List<User>
    {
        new User
        {
            Id = "1",
            UserName = "existingUser",
            Email = "existing@example.com",
            PhoneNumber = "0987654321"
        }
    };

            // Mock UserRepo
            var userRepoMock = new Mock<IUserRepository>();
            userRepoMock.Setup(r => r.Query())
                .Returns(MockDbSetProvider.CreateMockDbSet(fakeUsers));
            userRepoMock.Setup(r => r.UserManager).Returns(_userManagerMock.Object);

            // Mock ProfileRepo
            var profileRepoMock = new Mock<IProfileRepository>();
            profileRepoMock.Setup(r => r.AddAsync(It.IsAny<PMS.Core.Domain.Entities.Profile>()))
                .Returns(Task.CompletedTask);

            // Mock CustomerProfileRepo
            var customerProfileRepoMock = new Mock<ICustomerProfileRepository>();
            customerProfileRepoMock.Setup(r => r.AddAsync(It.IsAny<CustomerProfile>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Profile).Returns(profileRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.CustomerProfile).Returns(customerProfileRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
                .ReturnsAsync("token123");
            _emailServiceMock.Setup(e => e.SendMailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _userService.RegisterUserAsync(registerRequest);

            // Assert
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), registerRequest.ConfirmPassword), Times.Once);
            _userManagerMock.Verify(x =>
                x.AddToRoleAsync(It.IsAny<User>(), It.Is<string>(r => r.Equals("Customer", StringComparison.OrdinalIgnoreCase))),
                Times.Once);
            _emailServiceMock.Verify(x => x.SendMailAsync(It.IsAny<string>(), It.IsAny<string>(), registerRequest.Email), Times.Once);
        }

        [Fact]
        public async Task SendEmailResetPasswordAsync_ShouldSendEmail_WhenUserConfirmed()
        {
            // Arrange
            var user = new User { Id = "1", Email = "test@example.com" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email))
                .ReturnsAsync(user);

            _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user))
                .ReturnsAsync(true);

            _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("resetToken");

            _emailServiceMock.Setup(e => e.SendMailAsync(It.IsAny<string>(), It.IsAny<string>(), user.Email))
                .Returns(Task.CompletedTask);

            // Act
            await _userService.SendEmailResetPasswordAsync(user.Email);

            // Assert
            _emailServiceMock.Verify(e => e.SendMailAsync(It.IsAny<string>(), It.IsAny<string>(), user.Email), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldResetPassword_WhenValidToken()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                UserId = "1",
                Token = "token123",
                NewPassword = "NewPass@123"
            };

            var user = new User { Id = "1", Email = "test@example.com" };

            _userManagerMock.Setup(x => x.FindByIdAsync(request.UserId))
                .ReturnsAsync(user);

            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, request.Token, request.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _userService.ResetPasswordAsync(request);

            // Assert
            _userManagerMock.Verify(x => x.ResetPasswordAsync(user, request.Token, request.NewPassword), Times.Once);
        }
        [Fact]
        public async Task ReSendEmailConfirmAsync_ShouldSendEmail_WhenUserExists()
        {
            var request = new ResendConfirmEmailRequest { EmailOrUsername = "test@example.com" };
            var user = new User { Id = "1", Email = "test@example.com" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(request.EmailOrUsername)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.UpdateSecurityStampAsync(user)).ReturnsAsync(IdentityResult.Success);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _userManagerMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("token");
            _emailServiceMock.Setup(e => e.SendMailAsync(It.IsAny<string>(), It.IsAny<string>(), user.Email))
                .Returns(Task.CompletedTask);

            await _userService.ReSendEmailConfirmAsync(request);

            _emailServiceMock.Verify(e => e.SendMailAsync(It.IsAny<string>(), It.IsAny<string>(), user.Email), Times.Once);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ShouldThrow_WhenUserAlreadyConfirmed()
        {
            var user = new User { Id = "1", EmailConfirmed = true };
            _userManagerMock.Setup(u => u.FindByIdAsync(user.Id)).ReturnsAsync(user);

            await Assert.ThrowsAsync<Exception>(() => _userService.ConfirmEmailAsync(user.Id, "token"));
        }

        [Fact]
        public async Task GetUserRoles_ShouldReturnRoles()
        {
            var user = new User();
            var roles = new List<string> { "Customer", "Admin" };
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);

            var result = await _userService.GetUserRoles(user);

            Assert.Equal(roles, result);
        }
    }
}

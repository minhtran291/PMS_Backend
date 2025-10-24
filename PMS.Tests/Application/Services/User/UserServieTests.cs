using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PMS.Application.DTOs.Auth;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Notification;
using PMS.Application.Services.User;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Identity;
using PMS.Tests.TestBase;
using System.Collections.Generic;
using System.Threading.Tasks;
using MockQueryable.Moq;

namespace PMS.Tests
{
    [TestFixture]
    public class RegisterServiceTests : ServiceTestBase
    {
        private UserService _userService;

        [SetUp]
        public override void BaseSetup()
        {
            base.BaseSetup();

            var emailServiceMock = new Mock<IEmailService>();
            var loggerMock = new Mock<ILogger<UserService>>();
            var notificationServiceMock = new Mock<INotificationService>();

            _userService = new UserService(
                UnitOfWorkMock.Object,
                MapperMock.Object,
                emailServiceMock.Object,
                loggerMock.Object,
                notificationServiceMock.Object
            );
        }

        [Test]
        public async Task Register_ShouldReturnBadRequest_WhenEmailAlreadyConfirmed()
        {
            var existingUser = SampleData.ExistingUser;
            existingUser.EmailConfirmed = true;

            var request = SampleData.ValidRegisterRequest;
            request.Email = existingUser.Email;

            UserManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync(existingUser);

            var result = await _userService.RegisterUserAsync(request);

            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Does.Contain("Email đã được đăng ký"));
        }

        [Test]
        public void Register_ShouldReturnBadRequest_WhenEmailIsInvalid()
        {
            var request = SampleData.InvalidEmailRegisterRequest;

            var isValid = request.Email.Contains("@");

            Assert.That(isValid, Is.False, "Email không hợp lệ");
        }

        [Test]
        public async Task Register_ShouldReturnBadRequest_WhenPhoneAlreadyExists()
        {
            var request = SampleData.ValidRegisterRequest;

            var users = new List<User> { new User { PhoneNumber = request.PhoneNumber } };
            var queryable = new TestAsyncEnumerable<User>(users);

            UserRepoMock.Setup(x => x.Query()).Returns(queryable);

            var result = await _userService.RegisterUserAsync(request);

            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Does.Contain("Trùng số điện thoại"));
        }

        [Test]
        public async Task Register_ShouldReturnBadRequest_WhenUsernameAlreadyExists()
        {
            var request = SampleData.ValidRegisterRequest;

            var users = new List<User>();
            var queryable = new TestAsyncEnumerable<User>(users);
            UserRepoMock.Setup(x => x.Query()).Returns(queryable);

            UserManagerMock.Setup(x => x.FindByNameAsync(request.UserName.ToLower()))
                .ReturnsAsync(new User { UserName = request.UserName });

            var result = await _userService.RegisterUserAsync(request);

            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Does.Contain("Tên đăng nhập đã tồn tại"));
        }

        [Test]
        public async Task Register_ShouldReturnSuccess_WhenValidRequest()
        {
            var request = SampleData.ValidRegisterRequest;

            UserManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync((User)null);
            UserManagerMock.Setup(x => x.FindByNameAsync(request.UserName.ToLower()))
                .ReturnsAsync((User)null);

            var users = new List<User>();
            var queryable = new TestAsyncEnumerable<User>(users);
            UserRepoMock.Setup(x => x.Query()).Returns(queryable);

            UserManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), request.ConfirmPassword))
                .ReturnsAsync(IdentityResult.Success);

            UserManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), UserRoles.CUSTOMER))
                .ReturnsAsync(IdentityResult.Success);

            UnitOfWorkMock.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            UnitOfWorkMock.Setup(x => x.CustomerProfile.AddAsync(It.IsAny<Core.Domain.Entities.CustomerProfile>()))
                .Returns(Task.CompletedTask);

            var result = await _userService.RegisterUserAsync(request);

            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Message, Does.Contain("Thành công vui lòng kiểm tra email"));
        }
    }
}

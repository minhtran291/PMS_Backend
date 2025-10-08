using Xunit;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using PMS.API.Services.Admin;
using PMS.Core.Domain.Identity;
using PMS.Core.DTO.Admin;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Constant;
using PMS.Data.UnitOfWork;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using PMS.Tests.Services;
using Profile = PMS.Core.Domain.Entities.Profile;

namespace PMS_Backend.Tests.Services.Admin
{
    public class AdminServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<AdminService>> _mockLogger;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly AdminService _adminService;

        public AdminServiceTests()
        {
            // Mock UserManager (vì Identity không có interface)
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null, null, null, null, null, null, null, null);

            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<AdminService>>();

            // Mock UnitOfWork.Users để trả về mock UserManager
            _mockUnitOfWork.Setup(u => u.Users.UserManager).Returns(_mockUserManager.Object);

            // Tạo service
            _adminService = new AdminService(_mockUnitOfWork.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldThrow_WhenEmailExists()
        {
            // Arrange
            var request = new CreateAccountRequest
            {
                Email = "existing@mail.com",
                PhoneNumber = "0901234567",
                Password = "Password123!",
                UserName = "staff01",
                FullName = "Staff 01"
            };

            _mockUserManager
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(new User());

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _adminService.CreateAccountAsync(request));
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldCreateSuccessfully_WhenValidRequest()
        {
            // Arrange
            var request = new CreateAccountRequest
            {
                Email = "newstaff@mail.com",
                PhoneNumber = "0912345678",
                Password = "Password123!",
                UserName = "staff02",
                FullName = "Staff 02",
                StaffRole = StaffRole.SalesStaff
            };

            _mockUnitOfWork.Setup(u => u.Users.UserManager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUnitOfWork.Setup(u => u.Users.UserManager.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUnitOfWork.Setup(u => u.Users.UserManager.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            _mockUnitOfWork.Setup(u => u.Users.Query())
                .Returns(MockDbSetProvider.CreateMockDbSet(new List<User>()));

            _mockUnitOfWork.Setup(u => u.Profile.AddAsync(It.IsAny<Profile>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.StaffProfile.AddAsync(It.IsAny<StaffProfile>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);


            // Act
            await _adminService.CreateAccountAsync(request);

            // Assert
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<User>(), request.Password), Times.Once);
            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), UserRoles.SALES_STAFF), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAccountAsync_ShouldUpdateProfileAndStaffProfile()
        {
            // Arrange
            var user = new User
            {
                Id = "1",
                PhoneNumber = "0900000000",
                UserStatus = UserStatus.Active,
                Profile = new Profile
                {
                    Id = 10,
                    FullName = "Old Name",
                    Avatar = "old.png",
                    Gender = Gender.Male,
                    Address = "Old Address",
                    StaffProfile = new StaffProfile
                    {
                        Id = 20,
                        EmployeeCode = "EMP001",
                        Department = "Sales",
                        Notes = "Old notes"
                    }
                }
            };

            var request = new UpdateAccountRequest
            {
                UserId = "1",
                FullName = "New Name",
                Address = "New Address",
                Department = "HR",
                Notes = "Updated notes",
                PhoneNumber = "0911111111",
                Gender = Gender.FeMale,
                UserStatus = UserStatus.Active
            };

            var users = new List<User> { user };

            _mockUnitOfWork.Setup(u => u.Users.Query())
                .Returns(MockDbSetProvider.CreateMockDbSet(users));

            // Mock các phương thức UnitOfWork khác
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _mockUnitOfWork.Setup(u => u.Profile.Update(It.IsAny<Profile>()));
            _mockUnitOfWork.Setup(u => u.StaffProfile.Update(It.IsAny<StaffProfile>()));

            // Act
            await _adminService.UpdateAccountAsync(request);

            // Assert
            Assert.Equal("New Name", user.Profile.FullName);
            Assert.Equal("New Address", user.Profile.Address);
            Assert.Equal(Gender.FeMale, user.Profile.Gender);
            Assert.Equal("HR", user.Profile.StaffProfile.Department);
            Assert.Equal("Updated notes", user.Profile.StaffProfile.Notes);
            Assert.Equal("0911111111", user.PhoneNumber);

            _mockUnitOfWork.Verify(u => u.Profile.Update(It.IsAny<Profile>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.StaffProfile.Update(It.IsAny<StaffProfile>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }


        [Fact]
        public async Task GetAccountListAsync_ShouldReturnList_WhenUsersExist()
        {
            // Arrange
            var users = new List<User>
    {
        new User
        {
            Id = "1",
            Email = "staff1@mail.com",
            PhoneNumber = "0900000000",
            UserStatus = UserStatus.Active,
            CreateAt = DateTime.UtcNow,
            Profile = new Profile
            {
                Id = 10,
                FullName = "Staff 1",
                Gender = Gender.Male,
                StaffProfile = new StaffProfile { Department = "Sales" }
            }
        },
        new User
        {
            Id = "2",
            Email = "staff2@mail.com",
            PhoneNumber = "0911111111",
            UserStatus = UserStatus.Active,
            CreateAt = DateTime.UtcNow.AddMinutes(-1),
            Profile = new Profile
            {
                Id = 20,
                FullName = "Staff 2",
                Gender = Gender.FeMale,
                StaffProfile = new StaffProfile { Department = "HR" }
            }
        }
    };

            _mockUnitOfWork.Setup(u => u.Users.Query())
                .Returns(MockDbSetProvider.CreateMockDbSet(users));

            // Act
            var result = await _adminService.GetAccountListAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Email == "staff1@mail.com");
        }

        [Fact]
        public async Task GetAccountDetailAsync_ShouldReturnAccountDetails_WhenIdExists()
        {
            // Arrange
            var user = new User
            {
                Id = "1",
                Email = "staff1@mail.com",
                PhoneNumber = "0900000000",
                UserStatus = UserStatus.Active,
                CreateAt = DateTime.UtcNow,
                Profile = new Profile
                {
                    Id = 10,
                    FullName = "Staff 1",
                    Gender = Gender.Male,
                    Address = "HCM",
                    StaffProfile = new StaffProfile
                    {
                        Id = 20,
                        EmployeeCode = "EMP001",
                        Department = "Sales",
                        Notes = "Test notes"
                    },
                    CustomerProfile = null
                }
            };

            var users = new List<User> { user };
            _mockUnitOfWork.Setup(u => u.Users.Query())
                .Returns(MockDbSetProvider.CreateMockDbSet(users));

            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(new List<string> { "SalesStaff" });

            // Act
            var result = await _adminService.GetAccountDetailAsync("1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1", result.UserId);
            Assert.Equal("staff1@mail.com", result.Email);
            Assert.Equal("Staff 1", result.FullName);
            Assert.Equal("Sales", result.Department);
        }


        [Fact]
        public async Task SuspendAccountAsync_ShouldUpdateUserStatus()
        {
            // Arrange
            var user = new User { Id = "1", UserStatus = UserStatus.Active };
            _mockUserManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            await _adminService.SuspendAccountAsync("1");

            // Assert
            Assert.Equal(UserStatus.Block, user.UserStatus);
            _mockUserManager.Verify(m => m.UpdateAsync(user), Times.Once);
        }
    }
}

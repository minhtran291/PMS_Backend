using PMS.Application.DTOs.Auth;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using System.Collections.Generic;

public static class SampleData
{
    // User hiện tại
    public static User ExistingUser = new User
    {
        Id = "USER-001",
        UserName = "testuser",
        Email = "test@example.com",
        EmailConfirmed = true,
        UserStatus = UserStatus.Active,
        PasswordHash = "hashed_password",
        PhoneNumber = "0123456789",
        Address = "123 Test Street",
    };

    // Login request hợp lệ
    public static LoginRequest ValidLoginRequest => new LoginRequest
    {
        UsernameOrEmail = "test@example.com",
        Password = "Test@123"
    };

    // Register request hợp lệ
    public static RegisterUser ValidRegisterRequest => new RegisterUser
    {
        UserName = "tunahe171966",
        FullName = "Nguyen Anh Tu",
        Email = "tunahe171966@fpt.edu.vn",
        Password = "Tunahe171966@",
        ConfirmPassword = "Tunahe171966@",
        PhoneNumber = "0987654321",
        Address = "Hanoi"
    };

    // Register request email không hợp lệ
    public static RegisterUser InvalidEmailRegisterRequest => new RegisterUser
    {
        UserName = "tunahe171966",
        FullName = "Nguyen Anh Tu",
        Email = "tunahe171966.fpt.edu.vn",
        Password = "Tunahe171966@",
        ConfirmPassword = "Tunahe171966@",
        PhoneNumber = "0987654321",
        Address = "Hanoi"
    };

    // Các role mẫu
    public static IList<string> CustomerRoles = new List<string> { "Customer" };
    public static IList<string> StaffRoles = new List<string> { "Staff" };
}

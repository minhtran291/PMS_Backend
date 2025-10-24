    using AutoMapper;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Logging;
    using Moq;
    using PMS.Application.Services.Auth;
    using PMS.Application.Services.ExternalService;
    using PMS.Application.Services.Notification;
    using PMS.Application.Services.User;
    using PMS.Core.Domain.Identity;
    using PMS.Data.Repositories.User;
    using PMS.Data.UnitOfWork;

    namespace PMS.Tests.TestBase
    {
        public abstract class ServiceTestBase
        {
            protected Mock<IUnitOfWork> UnitOfWorkMock;
            protected Mock<IUserRepository> UserRepoMock;
            protected Mock<UserManager<User>> UserManagerMock;
            protected Mock<SignInManager<User>> SignInManagerMock;
            protected Mock<IUserService> UserServiceMock;
            protected Mock<ITokenService> TokenServiceMock;
            protected Mock<IMapper> MapperMock;
            protected Mock<IEmailService>? EmailServiceMock;
            protected Mock<INotificationService>? NotificationServiceMock;
            protected ILogger<UserService> LoggerUserService => Mock.Of<ILogger<UserService>>();

            protected ILogger<LoginService> LoggerMock => Mock.Of<ILogger<LoginService>>();

            [SetUp]
            public virtual void BaseSetup()
            {
                // Mock Identity managers
                UserManagerMock = MockHelper.MockUserManager();
                SignInManagerMock = MockHelper.MockSignInManager(UserManagerMock);

                // Mock user repository
                UserRepoMock = new Mock<IUserRepository>();
                UserRepoMock.SetupGet(x => x.UserManager).Returns(UserManagerMock.Object);
                UserRepoMock.SetupGet(x => x.SignInManager).Returns(SignInManagerMock.Object);
                EmailServiceMock = new Mock<IEmailService>();
                NotificationServiceMock = new Mock<INotificationService>();

                // Mock UnitOfWork
                UnitOfWorkMock = new Mock<IUnitOfWork>();
                UnitOfWorkMock.SetupGet(x => x.Users).Returns(UserRepoMock.Object);
                UnitOfWorkMock.Setup(x => x.CommitAsync()).ReturnsAsync(1);

                // Mock other services
                MapperMock = new Mock<IMapper>();
                UserServiceMock = new Mock<IUserService>();
                TokenServiceMock = new Mock<ITokenService>();
            }
        }
    }

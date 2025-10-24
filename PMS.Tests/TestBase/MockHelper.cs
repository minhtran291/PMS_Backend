using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using PMS.Core.Domain.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMS.Tests.TestBase
{
    public static class MockHelper
    {
        public static Mock<UserManager<User>> MockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            var mgr = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

            mgr.Object.UserValidators.Add(new UserValidator<User>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<User>());

            return mgr;
        }

        public static Mock<SignInManager<User>> MockSignInManager(Mock<UserManager<User>> userManagerMock)
        {
            return new Mock<SignInManager<User>>(
                userManagerMock.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                null, null, null, null);
        }
    }
}
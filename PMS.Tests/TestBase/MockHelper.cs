using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using PMS.Core.Domain.Identity;

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

        public static Mock<DbSet<T>> MockDbSet<T>(IEnumerable<T> data) where T : class
        {
            var queryableData = data.AsQueryable();
            var asyncEnumerable = new TestAsyncEnumerable<T>(queryableData);
            var asyncQueryable = asyncEnumerable.AsQueryable();

            var mock = new Mock<DbSet<T>>();

            
            mock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(asyncQueryable.Provider);
            mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(asyncQueryable.Expression);
            mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(asyncQueryable.ElementType);
            mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => asyncQueryable.GetEnumerator());

           
            mock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => asyncQueryable.AsAsyncEnumerable().GetAsyncEnumerator(ct));

           
            mock.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(entity =>
            {
                var list = data as IList<T>;
                list?.Add(entity);
            });

            mock.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Callback<T, CancellationToken>((entity, _) =>
                {
                    var list = data as IList<T>;
                    list?.Add(entity);
                })
                .ReturnsAsync((T entity, CancellationToken _) => (EntityEntry<T>)null!); 

            mock.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(entity =>
            {
                var list = data as IList<T>;
                list?.Remove(entity);
            });

            return mock;
        }

        // Extension tiện lợi
        public static Mock<DbSet<T>> ToMockDbSet<T>(this IEnumerable<T> source) where T : class
            => MockDbSet(source);
    }
}
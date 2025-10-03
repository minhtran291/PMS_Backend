using AutoMapper;
using PMS.API.Services.Base;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.User
{
    public class UserService(IUnitOfWork unitOfWork, IMapper mapper) : Service(unitOfWork, mapper), IUserService
    {
        public async Task<Core.Domain.Identity.User?> GetUserById(string userId)
        {
            return await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
        }

        public async Task<IList<string>> GetUserRoles(Core.Domain.Identity.User user)
        {
            return await _unitOfWork.Users.UserManager.GetRolesAsync(user);
        }
    }
}

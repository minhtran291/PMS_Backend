using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Configuration;
using Microsoft.AspNetCore.Identity;
using PMS.Data.UnitOfWork;

namespace PMS.Application.Services.Notification
{
    public class UserRoleResolverService(IUnitOfWork unitOfWork) : IUserRoleResolverService
    {
        public async Task<List<Core.Domain.Identity.User>> GetUsersByRolesAsync(List<string> roles)
        {
            var users = new List<Core.Domain.Identity.User>();

            foreach (var role in roles.Distinct())
            {
                var usersInRole = await unitOfWork.Users.UserManager.GetUsersInRoleAsync(role);
                users.AddRange(usersInRole);
            }

            return users.DistinctBy(u => u.Id).ToList();
        }
    }
}

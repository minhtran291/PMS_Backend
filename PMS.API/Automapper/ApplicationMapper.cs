using PMS.Core.DTO.Admin;
using DProfile = PMS.Core.Domain.Entities.Profile;

namespace PMS.API.Automapper
{
    public class ApplicationMapper : AutoMapper.Profile
    {
        public ApplicationMapper() 
        {
            CreateMap<AdminCreateAccountRequest, DProfile>();
        }
    }
}

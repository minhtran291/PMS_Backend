using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Application.DTOs.PO;
using PMS.Core.Domain.Constant;

namespace PMS.Application.Services.PO
{
    public interface IPOService
    {
        Task<ServiceResult<IEnumerable<POViewDTO>>> GetAllPOAsync();
        Task<ServiceResult<bool>> UpdatePOAsync(string userId, int poid, POUpdateDTO pOUpdateDTO);

        Task<ServiceResult<POViewDTO>> ViewDetailPObyID(int poid);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Application.DTOs.PO;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;

namespace PMS.Application.Services.PO
{
    public interface IPOService
    {
        Task<ServiceResult<IEnumerable<POViewDTO>>> GetAllPOAsync();
        Task<ServiceResult<POPaidViewDTO>> DepositedPOAsync(string userId, int poid, POUpdateDTO pOUpdateDTO);

        Task<ServiceResult<POViewDTO>> ViewDetailPObyID(int poid);

        Task<ServiceResult<POPaidViewDTO>> DebtAccountantPOAsync(string userId, int poid, POUpdateDTO pOUpdateDTO);

        Task<ServiceResult<bool>> ChangeStatusAsync(int poid, PurchasingOrderStatus newStatus);

    }
}

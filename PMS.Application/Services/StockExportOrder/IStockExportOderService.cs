using PMS.Application.DTOs.StockExportOrder;
using PMS.Core.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.StockExportOrder
{
    public interface IStockExportOderService
    {
        Task<ServiceResult<object>> CreateAsync(StockExportOrderDTO dto, string userId);
        Task<ServiceResult<object>> SendAsync(int seoId, string userId);
        Task<ServiceResult<object>> ListAsync(string userId);
        Task<ServiceResult<object>> DetailsAsync(int seoId, string userId);
        Task<ServiceResult<object>> UpdateAsync(UpdateStockExportOrderDTO dto, string userId);
        Task<ServiceResult<object>> DeleteAsync(int seoId, string userId);
        Task<ServiceResult<object>> GenerateForm(int soId);
        Task <ServiceResult<object>> CheckAvailable(int seoId);
        Task <ServiceResult<object>>AwaitStockExportOrder(int seoId, string userId);
        Task CancelStockExportOrder(int soId);
        Task<ServiceResult<object>> CancelSEOWithReturn(int seoId, string userId);
    }
}

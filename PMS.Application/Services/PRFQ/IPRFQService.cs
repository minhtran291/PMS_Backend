using Microsoft.AspNetCore.Http;
using PMS.Application.DTOs.PRFQ;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;


namespace PMS.API.Services.PRFQService
{
    public interface IPRFQService
    {
        Task<ServiceResult<int>> CreatePRFQAsync(string userId, int supplierId, string taxCode, string myPhone, string myAddress, List<int> productIds, PRFQStatus status);

        Task<ServiceResult<int>> ConvertExcelToPurchaseOrderAsync(string userId, PurchaseOrderInputDto input);
        Task<PreviewExcelResponse> PreviewExcelProductsAsync(IFormFile file);
        Task<ServiceResult<bool>> DeletePRFQAsync(int prfqId, string userId);

        Task<ServiceResult<object>> GetPRFQDetailAsync(int prfqId);

        Task<ServiceResult<IEnumerable<object>>> GetAllPRFQAsync();
    }
}

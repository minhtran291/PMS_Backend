using Microsoft.AspNetCore.Http;
using PMS.Application.DTOs.PRFQ;
using PMS.Core.Domain.Constant;


namespace PMS.API.Services.PRFQService
{
    public interface IPRFQService
    {
        Task<ServiceResult<int>> CreatePRFQAsync(string userId, int supplierId, string taxCode, string myPhone, string myAddress, List<int> productIds);

        Task<ServiceResult<int>> ConvertExcelToPurchaseOrderAsync(string userId, PurchaseOrderInputDto input);
        Task<PreviewExcelResponse> PreviewExcelProductsAsync(IFormFile file);
       
    }
}

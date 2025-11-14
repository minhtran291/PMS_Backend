using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.PO;
using PMS.Application.DTOs.PRFQ;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;


namespace PMS.API.Services.PRFQService
{
    public interface IPRFQService
    {
        Task<ServiceResult<int>> CreatePRFQAsync(string userId, int supplierId, string taxCode, string myPhone, string myAddress, List<int> productIds, PRFQStatus status);

        Task<ServiceResult<int>> ConvertExcelToPurchaseOrderAsync(string userId, PurchaseOrderInputDto input, PurchasingOrderStatus purchasingOrderStatus);
        Task<PreviewExcelResponse> PreviewExcelProductsAsync(IFormFile file);
        Task<ServiceResult<bool>> DeletePRFQAsync(int prfqId, string userId);

        Task<ServiceResult<object>> GetPRFQDetailAsync(int prfqId);

        Task<ServiceResult<IEnumerable<object>>> GetAllPRFQAsync();

        Task<byte[]> GenerateExcelAsync(int prfqId);

        Task<ServiceResult<object>> PreviewPRFQAsync(int id);
        Task<ServiceResult<bool>> UpdatePRFQStatusAsync(int prfqId, PRFQStatus newStatus);

        Task<ServiceResult<int>> ContinueEditPRFQ(int prfqId, ContinuePRFQDTO dto);

        Task<ServiceResult<int>> CreatePurchaseOrderByQIDAsync(string userId,
        PurchaseOrderByQuotaionInputDto input);

        Task<ServiceResult<bool>> CountinueEditPurchasingOrderAsync(int poid, string userid, PurchaseOrderByQuotaionInputDto input);

        Task<ServiceResult<IEnumerable<PreviewProductDto>>> PreviewExcelProductsByExcitedQuotationAsync(int QID);
    }


}

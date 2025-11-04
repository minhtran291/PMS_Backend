using PMS.Application.DTOs.GRN;
using PMS.Core.Domain.Constant;
using PMS.Core.DTO.Content;

namespace PMS.API.Services.GRNService
{
    public interface IGRNService
    {
        Task<ServiceResult<int>> CreateGoodReceiptNoteFromPOAsync(string userId, int poId, int WarehouseLocationID);

        Task<ServiceResult<bool>> CreateGRNByManually(string userId, int poId, GRNManuallyDTO GRNManuallyDTO);

        Task<ServiceResult<List<GRNViewDTO>>> GetAllGRN();

        Task<ServiceResult<GRNViewDTO>> GetGRNDetailAsync(int grnId);

        Task<byte[]> GeneratePDFGRNAsync(int grnId);
    }
}

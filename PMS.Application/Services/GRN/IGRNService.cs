using PMS.Core.Domain.Constant;
using PMS.Core.DTO.Content;

namespace PMS.API.Services.GRNService
{
    public interface IGRNService
    {
         Task<ServiceResult<int>> CreateGoodReceiptNoteFromPOAsync(string userId, int poId);

        Task<ServiceResult<bool>> CreateGRNByManually(string userId, int poId, GRNManuallyDTO GRNManuallyDTO);
    }
}

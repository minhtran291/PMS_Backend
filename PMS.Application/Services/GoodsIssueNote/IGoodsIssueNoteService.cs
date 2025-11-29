using PMS.Application.DTOs.GoodsIssueNote;
using PMS.Core.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.GoodsIssueNote
{
    public interface IGoodsIssueNoteService
    {
        Task<ServiceResult<object>> CreateAsync(CreateGoodsIssueNoteDTO dto, string userId);
        Task<ServiceResult<object>> SendAsync(int ginId, string userId);
        Task<ServiceResult<object>> ListAsync(string userId);
        Task<ServiceResult<object>> DetailsAsync(int ginId, string userId);
        Task<ServiceResult<object>> UpdateAsync(UpdateGoodsIssueNoteDTO dto, string userId);
        Task<ServiceResult<object>> DeleteAsync(int ginId, string userId);
        Task<ServiceResult<object>> WarningAsync();
        Task<ServiceResult<object>> ResponseNotEnough(int stockExportOrderId, string userId);
    }
}

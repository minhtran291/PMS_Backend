using PMS.Application.DTOs.VietQR;
using PMS.Core.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.VietQR
{
    public interface IVietQrService
    {
        Task<ServiceResult<VietQrInitResponse>> InitAsync(VietQrInitRequest req);
        Task<ServiceResult<bool>> ConfirmAsync(VietQrConfirmRequest req);
    }
}

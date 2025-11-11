using Microsoft.AspNetCore.Http;
using PMS.Application.DTOs.VnPay;
using PMS.Core.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.VNpay
{
    public interface IVnPayService
    {
        Task<ServiceResult<VnPayInitResponseDTO>> InitVnPayAsync(VnPayInitRequestDTO req, string clientIp);
        Task<ServiceResult<bool>> HandleVnPayReturnAsync(IQueryCollection query);
        Task<ServiceResult<bool>> HandleVnPayIpnAsync(IQueryCollection query);
    }
}

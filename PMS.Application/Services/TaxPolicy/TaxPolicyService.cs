using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.TaxPolicy;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.TaxPolicy
{
    public class TaxPolicyService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<TaxPolicyService> logger) : Service(unitOfWork, mapper), ITaxPolicySerivce
    {
        private readonly ILogger<TaxPolicyService> _logger = logger;

        public async Task<ServiceResult<object>> CreateAsync(CreateTaxPolicyDTO dto)
        {
            try
            {
                var checkName = await _unitOfWork.TaxPolicy.Query()
                .AnyAsync(t => t.Name.ToLower() == dto.Name.Trim().ToLower());

                if (checkName)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Tên thuế xuất đã tồn tại"
                    };

                var checkRate = await _unitOfWork.TaxPolicy.Query()
                    .AnyAsync(t => t.Rate == dto.Rate);

                if (checkRate)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Tỉ lệ phần trăm thuế xuất đã tồn tại"
                    };

                var tax = new Core.Domain.Entities.TaxPolicy
                {
                    Name = dto.Name.Trim(),
                    Rate = dto.Rate,
                    Description = dto.Description,
                    Status = dto.Status,
                };

                await _unitOfWork.TaxPolicy.AddAsync(tax);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 201,
                    Message = "Tạo thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<object>> UpdateAsync(UpdateTaxPolicyDTO dto)
        {
            try
            {
                var tax = await _unitOfWork.TaxPolicy.Query()
                    .Include(t => t.SalesQuotaionDetails)
                    .FirstOrDefaultAsync(t => t.Id == dto.Id);

                if (tax == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Thuế xuất không tồn tại"
                    };

                if (tax.Status == true)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Thuế đang được sử dụng không thể cập nhật"
                    };

                if (tax.SalesQuotaionDetails.Count() == 0)
                {
                    var checkName = await _unitOfWork.TaxPolicy.Query()
                    .AnyAsync(t => t.Name.ToLower() == dto.Name.Trim().ToLower() && t.Id != tax.Id);

                    if (checkName)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = "Tên thuế xuất đã tồn tại"
                        };

                    var checkRate = await _unitOfWork.TaxPolicy.Query()
                            .AnyAsync(t => t.Rate == dto.Rate && t.Id != tax.Id);

                    if (checkRate)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = "Tỉ lệ phần trăm thuế xuất đã tồn tại"
                        };

                    tax.Name = dto.Name.Trim();
                    tax.Rate = dto.Rate;
                    tax.Description = dto.Description;
                }
                else if(tax.SalesQuotaionDetails.Count > 0)
                {
                    tax.Description = dto.Description;
                } 

                _unitOfWork.TaxPolicy.Update(tax);

                await _unitOfWork.CommitAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Cập nhật thành công"
                };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Loi");

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<object>> DisableEnableAsync(int taxId)
        {
            try
            {
                var tax = await _unitOfWork.TaxPolicy.Query()
                    .FirstOrDefaultAsync(t => t.Id == taxId);

                if (tax == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Thuế xuất không tồn tại"
                    };

                tax.Status = !tax.Status;

                _unitOfWork.TaxPolicy.Update(tax);

                await _unitOfWork.CommitAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Loi");

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<object>> DeleteAsync(int taxId)
        {
            try
            {
                var tax = await _unitOfWork.TaxPolicy.Query()
                    .Include(t => t.SalesQuotaionDetails)
                    .FirstOrDefaultAsync(t => t.Id == taxId);

                if (tax == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Thuế xuất không tồn tại"
                    };

                if (tax.Status == true)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Thuế đang được sử dụng không thể xóa"
                    };

                if(tax.SalesQuotaionDetails.Count > 0)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Thuế đã được sử dụng trong báo giá không thể xóa"
                    };

                _unitOfWork.TaxPolicy.Remove(tax);

                await _unitOfWork.CommitAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Xóa thành công"
                };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Loi");

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<object>> ListAsync()
        {
            try
            {
                var query = await _unitOfWork.TaxPolicy.Query()
                    .AsNoTracking()
                    .ToListAsync();
                
                var result = _mapper.Map<List<TaxPolicyDTO>>(query);

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Data = result
                };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Loi");

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<object>> DetailsAsync(int taxId)
        {
            try
            {
                var tax = await _unitOfWork.TaxPolicy.Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == taxId);

                if (tax == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Thuế xuất không tồn tại"
                    };

                var result = _mapper.Map<TaxPolicyDTO>(tax);

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }
    }
}

using AutoMapper;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.Product;
using PMS.Application.DTOs.Supplier;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Data.UnitOfWork;
using System.ComponentModel.DataAnnotations;
using DA = System.ComponentModel.DataAnnotations;
using RSupplier = PMS.Core.Domain.Entities.Supplier;


namespace PMS.Application.Services.Supplier
{
    public class SupplierService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IDataProtectionProvider protectionProvider,
        ILogger<SupplierService> logger
    ) : Service(unitOfWork, mapper), ISupplierService
    {

        private readonly IDataProtector _protector = protectionProvider.CreateProtector("PMS.Supplier.BankAccountNumber");
        private readonly ILogger<SupplierService> _logger = logger;

        public async Task<ServiceResult<SupplierResponseDTO>> CreateAsync(CreateSupplierRequestDTO dto)
        {
            var ctx = new DA.ValidationContext(dto);
            var results = new List<ValidationResult>();
            if (!DA.Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true))
            {
                var msg = string.Join(" | ", results.Select(r => r.ErrorMessage));
                return new ServiceResult<SupplierResponseDTO>
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = string.IsNullOrWhiteSpace(msg) ? "Dữ liệu không hợp lệ" : msg,
                    Data = null
                };
            }
            try
            {
                var dup = await _unitOfWork.Supplier.Query()
                    .AnyAsync(s => s.Email == dto.Email || s.PhoneNumber == dto.PhoneNumber);

                if (dup)
                {
                    return new ServiceResult<SupplierResponseDTO>
                    {
                        StatusCode = 400,
                        Message = "Email hoặc SĐT đã được sử dụng",
                        Data = null
                    };
                }

                var e = new RSupplier
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    Status = Core.Domain.Enums.SupplierStatus.Active,
                    BankAccountNumber = _protector.Protect(dto.BankAccountNumber),
                    MyDebt = dto.MyDebt
                };

                await _unitOfWork.Supplier.AddAsync(e);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<SupplierResponseDTO>
                {
                    StatusCode = StatusCodes.Status201Created,
                    Message = "Tạo nhà cung cấp thành công",
                    Data = ToResponse(e)
                };
        }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create supplier failed");
                return new ServiceResult<SupplierResponseDTO>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra khi tạo nhà cung cấp",
                    Data = null
                };
}
        }

        public async Task<ServiceResult<bool>> DisableSupplier(string supplierId)
        {
            try
            {
                if (!int.TryParse(supplierId, out var id))
                    return new ServiceResult<bool> { 
                        StatusCode = 400, 
                        Message = "supplierId không hợp lệ", 
                        Data = false };

                var e = await _unitOfWork.Supplier.Query().FirstOrDefaultAsync(x => x.Id == id);
                if (e == null)
                    return new ServiceResult<bool> { 
                        StatusCode = 404, 
                        Message = "Không tìm thấy nhà cung cấp", 
                        Data = false };

                e.Status = Core.Domain.Enums.SupplierStatus.Inactive;
                _unitOfWork.Supplier.Update(e);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<bool> { 
                    StatusCode = 200, 
                    Message = "Vô hiệu hoá thành công", 
                    Data = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vô hiệu hóa nhà cung cấp thất bại");
                return new ServiceResult<bool> { 
                    StatusCode = 500, 
                    Message = "Có lỗi xảy ra", 
                    Data = false };
            }
        }

        public async Task<ServiceResult<bool>> EnableSupplier(string supplierId)
        {
            try
            {
                if (!int.TryParse(supplierId, out var id))
                    return new ServiceResult<bool> { 
                        StatusCode = 400, 
                        Message = "supplierId không hợp lệ", 
                        Data = false };

                var e = await _unitOfWork.Supplier.Query().FirstOrDefaultAsync(x => x.Id == id);
                if (e == null)
                    return new ServiceResult<bool> { 
                        StatusCode = 404, 
                        Message = "Không tìm thấy nhà cung cấp", 
                        Data = false };

                e.Status = Core.Domain.Enums.SupplierStatus.Active;
                _unitOfWork.Supplier.Update(e);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<bool> { 
                    StatusCode = 200, 
                    Message = "Kích hoạt thành công", 
                    Data = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enable supplier failed");
                return new ServiceResult<bool> { 
                    StatusCode = 500, 
                    Message = "Có lỗi xảy ra", 
                    Data = false };
            }
        }

        public async Task<ServiceResult<SupplierResponseDTO>> GetByIdAsync(int id)
        {
            try
            {
                var e = await _unitOfWork.Supplier.Query().AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (e == null)
                    return new ServiceResult<SupplierResponseDTO> { 
                        StatusCode = 404, 
                        Message = "Không tìm thấy nhà cung cấp", 
                        Data = null };

                return new ServiceResult<SupplierResponseDTO>
                {
                    StatusCode = 200,
                    Message = "Lấy thông tin thành công",
                    Data = ToResponse(e)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get supplier by id failed");
                return new ServiceResult<SupplierResponseDTO> { StatusCode = 500, Message = "Có lỗi xảy ra", Data = null };
            }
        }

        public async Task<ServiceResult<List<SupplierResponseDTO>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 200);

                var q = _unitOfWork.Supplier.Query().AsNoTracking();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var k = keyword.Trim();
                    q = q.Where(s => s.Name.Contains(k)
                                  || s.Email.Contains(k)
                                  || s.PhoneNumber.Contains(k)
                                  || s.Address.Contains(k));
                }

                var list = await q.OrderByDescending(s => s.Id)
                                  .Skip((page - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();

                var data = list.Select(ToResponse).ToList();

                return new ServiceResult<List<SupplierResponseDTO>>
                {
                    StatusCode = data.Count > 0 ? 200 : 404,
                    Message = data.Count > 0 ? "Lấy danh sách thành công" : "Không có dữ liệu",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get paged suppliers failed");
                return new ServiceResult<List<SupplierResponseDTO>>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<List<LotProductDTOBySup>>> ListProductBySupId(string supplierId)
        {
            if (!int.TryParse(supplierId, out int supId))
                return ServiceResult<List<LotProductDTOBySup>>.Fail("SupplierId không hợp lệ");

            var result = await _unitOfWork.LotProduct.Query()
                .Include(lp => lp.Product)
                .Where(lp => lp.SupplierID == supId)
                .Select(lp => new LotProductDTOBySup
                {
                    ProductID = lp.ProductID,
                    ProductName = lp.Product.ProductName,
                    InputPrice = lp.InputPrice,
                    ExpiredDate = lp.ExpiredDate.ToString("yyyy-MM-dd"),
                    LotQuantity = lp.LotQuantity,
                    WarehouselocationID = lp.WarehouselocationID
                })
                .ToListAsync();
            if(result.Count < 0)
            {
                return new ServiceResult<List<LotProductDTOBySup>>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy bất kỳ sản phẩm nào",
                    Data = null
                };
            }

            return ServiceResult<List<LotProductDTOBySup>>.SuccessResult(result);
        }

        public async Task<ServiceResult<SupplierResponseDTO>> UpdateAsync(int id, UpdateSupplierRequestDTO dto)
        {
            try
            {
                var e = await _unitOfWork.Supplier.Query().FirstOrDefaultAsync(x => x.Id == id);
                if (e == null)
                    return new ServiceResult<SupplierResponseDTO> { 
                        StatusCode = 404, 
                        Message = "Không tìm thấy nhà cung cấp", 
                        Data = null };

                if (dto.Email != null || dto.PhoneNumber != null)
                {
                    var dup = await _unitOfWork.Supplier.Query().AnyAsync(x =>
                        x.Id != id &&
                        ((dto.Email != null && x.Email == dto.Email) ||
                         (dto.PhoneNumber != null && x.PhoneNumber == dto.PhoneNumber)));

                    if (dup)
                        return new ServiceResult<SupplierResponseDTO>
                        {
                            StatusCode = 200,
                            Message = "Email hoặc SĐT đã được sử dụng",
                            Data = null
                        };
                }

                if (dto.Name != null) e.Name = dto.Name;
                if (dto.Email != null) e.Email = dto.Email;
                if (dto.PhoneNumber != null) e.PhoneNumber = dto.PhoneNumber;
                if (dto.Address != null) e.Address = dto.Address;
                e.Status = dto.Status;
                if (dto.MyDebt != null) e.MyDebt = dto.MyDebt;
                if (dto.BankAccountNumber != null) e.BankAccountNumber = _protector.Protect(dto.BankAccountNumber);

                _unitOfWork.Supplier.Update(e);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<SupplierResponseDTO>
                {
                    StatusCode = 200,
                    Message = "Cập nhật thành công",
                    Data = ToResponse(e)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update supplier failed");
                return new ServiceResult<SupplierResponseDTO>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra khi cập nhật",
                    Data = null
                };
            }
        }

        private SupplierResponseDTO ToResponse(RSupplier s)
        {
            string Mask(string enc)
            {
                if (string.IsNullOrEmpty(enc)) return "";
                try
                {
                    var plain = _protector.Unprotect(enc);
                    var last4 = plain.Length >= 4 ? plain[^4..] : plain;
                    return new string('*', Math.Max(0, plain.Length - last4.Length)) + last4;
                }
                catch { return "***invalid***"; }
            }

            return new SupplierResponseDTO
            {
                Id = s.Id,
                Name = s.Name,
                Email = s.Email,
                PhoneNumber = s.PhoneNumber,
                Address = s.Address,
                Status = s.Status,
                BankAccountNumberMasked = Mask(s.BankAccountNumber),
                MyDebt = s.MyDebt
            };
        }
    }
}

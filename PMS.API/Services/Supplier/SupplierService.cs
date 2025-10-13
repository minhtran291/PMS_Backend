using AutoMapper;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PMS.API.Services.Admin;
using PMS.API.Services.Base;
using PMS.Core.DTO.Supplier;
using PMS.Data.UnitOfWork;
using RSupplier = PMS.Core.Domain.Entities.Supplier;

namespace PMS.API.Services.Supplier
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

        public async Task<SupplierResponseDTO> CreateAsync(CreateSupplierRequestDTO dto)
        {
            var dup = await _unitOfWork.Supplier.Query()
               .AnyAsync(s => s.Email == dto.Email || s.PhoneNumber == dto.PhoneNumber);
            if (dup) throw new Exception("Email hoặc SĐT đã tồn tại");

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
            return ToResponse(e);
        }

        public async Task DisableSupplier(string supplierId)
        {
            if (!int.TryParse(supplierId, out var id)) throw new ArgumentException("supplierId không hợp lệ");
            var e = await _unitOfWork.Supplier.Query()
           .FirstOrDefaultAsync(x => x.Id == id) ?? throw new Exception("Không tìm thấy nhà cung cấp");
            e.Status = Core.Domain.Enums.SupplierStatus.Inactive;
            _unitOfWork.Supplier.Update(e);
            await _unitOfWork.CommitAsync();
        }

        public async Task EnableSupplier(string supplierId)
        {
            if (!int.TryParse(supplierId, out var id)) throw new ArgumentException("supplierId không hợp lệ");
            var e = await _unitOfWork.Supplier.Query()
           .FirstOrDefaultAsync(x => x.Id == id) ?? throw new Exception("Không tìm thấy nhà cung cấp");
            e.Status = Core.Domain.Enums.SupplierStatus.Active;
            _unitOfWork.Supplier.Update(e);
            await _unitOfWork.CommitAsync();
        }

        public async Task<SupplierResponseDTO?> GetByIdAsync(int id)
        {
            var e = await _unitOfWork.Supplier.Query().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            return e == null ? null : ToResponse(e);
        }

        public async Task<IReadOnlyList<SupplierResponseDTO>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
            var q = _unitOfWork.Supplier.Query().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                q = q.Where(s => s.Name.Contains(k) || s.Email.Contains(k) ||
                                 s.PhoneNumber.Contains(k) || s.Address.Contains(k));
            }

            var list = await q.OrderByDescending(s => s.Id)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

            return list.Select(ToResponse).ToList();
        }

        public async Task<SupplierResponseDTO> UpdateAsync(int id, UpdateSupplierRequestDTO dto)
        {
            var e = await _unitOfWork.Supplier.Query().FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Không tìm thấy nhà cung cấp");

            if (dto.Email != null || dto.PhoneNumber != null)
            {
                var dup = await _unitOfWork.Supplier.Query().AnyAsync(x =>
                    x.Id != id &&
                    ((dto.Email != null && x.Email == dto.Email) ||
                     (dto.PhoneNumber != null && x.PhoneNumber == dto.PhoneNumber)));
                if (dup) throw new Exception("Email hoặc SĐT đã tồn tại");
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
            return ToResponse(e);
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

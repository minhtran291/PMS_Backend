using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.GoodsIssueNote;
using PMS.Application.Services.Base;
using PMS.Application.Services.Notification;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.GoodsIssueNote
{
    public class GoodsIssueNoteService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GoodsIssueNoteService> logger,
        INotificationService notificationService) : Service(unitOfWork, mapper), IGoodsIssueNoteService
    {
        private readonly ILogger<GoodsIssueNoteService> _logger = logger;
        private readonly INotificationService _notificationService = notificationService;

        public async Task<ServiceResult<object>> CreateAsync(CreateGoodsIssueNoteDTO dto, string userId)
        {
            try
            {
                var seo = await _unitOfWork.StockExportOrder.Query()
                    .Include(s => s.StockExportOrderDetails)
                        .ThenInclude(d => d.LotProduct)
                            .ThenInclude(l => l.WarehouseLocation)
                                .ThenInclude(w => w.Warehouse)
                    .Include(s => s.StockExportOrderDetails)
                        .ThenInclude(d => d.LotProduct)
                            .ThenInclude(lp => lp.Product)
                    .FirstOrDefaultAsync(s => s.Id == dto.StockExportOrderId);

                if (seo == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Lệnh xuất kho không tồn tại"
                    };

                if (seo.Status == Core.Domain.Enums.StockExportOrderStatus.Draft)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Lệnh xuất kho chưa được gửi không thể tạo phiếu xuất"
                    };

                bool existed = await _unitOfWork.GoodsIssueNote.Query()
                    .AnyAsync(g => g.StockExportOrderId == seo.Id);

                if (existed)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Lệnh xuất kho này đã có phiếu xuất"
                    };

                var allLots = await _unitOfWork.LotProduct.Query()
                    .Include(l => l.WarehouseLocation)
                        .ThenInclude(wl => wl.Warehouse)
                    .ToListAsync();

                var lotExportMap = new Dictionary<int, int>();

                var warehouseMap = new Dictionary<int, List<GoodsIssueNoteDetails>>();

                foreach (var item in seo.StockExportOrderDetails)
                {
                    var groups = allLots.Where(l => l.SalePrice == item.LotProduct.SalePrice
                                                && l.InputPrice == item.LotProduct.InputPrice
                                                && l.ExpiredDate == item.LotProduct.ExpiredDate
                                                && l.SupplierID == item.LotProduct.SupplierID
                                                && l.ProductID == item.LotProduct.ProductID)
                        .OrderBy(l => l.WarehouselocationID)
                        .ToList();

                    var requiredQuantity = item.Quantity;

                    foreach (var group in groups)
                    {
                        if (requiredQuantity <= 0)
                            break;

                        var export = Math.Min(requiredQuantity, group.LotQuantity);

                        requiredQuantity -= export;

                        if (!lotExportMap.ContainsKey(group.LotID))
                            lotExportMap[group.LotID] = 0;

                        lotExportMap[group.LotID] += export;

                        int warehouseId = group.WarehouseLocation.Warehouse.Id;

                        if (!warehouseMap.ContainsKey(warehouseId))
                            warehouseMap[warehouseId] = new List<GoodsIssueNoteDetails>();

                        warehouseMap[warehouseId].Add(new GoodsIssueNoteDetails
                        {
                            LotId = group.LotID,
                            Quantity = export,
                        });
                    }

                    if (requiredQuantity > 0)
                    {
                        {
                            return new ServiceResult<object>
                            {
                                StatusCode = 400,
                                Message = $"Không đủ hàng cho sản phẩm {item.LotProduct.Product.ProductName}"
                            };
                        }
                    }
                }

                var lotsToUpdate = allLots
                    .Where(l => lotExportMap.ContainsKey(l.LotID))
                    .ToList();

                foreach (var lot in lotsToUpdate)
                {
                    var totalExport = lotExportMap[lot.LotID];

                    if (lot.LotQuantity < totalExport)
                    {
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"Lô {lot.LotID} không đủ số lượng"
                        };
                    }

                    lot.LotQuantity -= totalExport;
                }

                var listGoodsIssueNote = new List<Core.Domain.Entities.GoodsIssueNote>();

                foreach (var wh in warehouseMap)
                {
                    var gin = new Core.Domain.Entities.GoodsIssueNote
                    {
                        StockExportOrderId = seo.Id,
                        GoodsIssueNoteCode = GenerateGoodsIssueNoteCode(),
                        CreateBy = userId,
                        WarehouseId = wh.Key,
                        CreateAt = DateTime.Now,
                        ExportedAt = DateTime.Now,
                        DeliveryDate = seo.DueDate,
                        Note = dto.Note,
                        Status = Core.Domain.Enums.GoodsIssueNoteStatus.Sent,
                        GoodsIssueNoteDetails = wh.Value
                    };

                    listGoodsIssueNote.Add(gin);
                }

                seo.Status = Core.Domain.Enums.StockExportOrderStatus.Exported;

                await _unitOfWork.BeginTransactionAsync();

                await _unitOfWork.GoodsIssueNote.AddRangeAsync(listGoodsIssueNote);

                _unitOfWork.StockExportOrder.Update(seo);

                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();

                await _notificationService.SendNotificationToRolesAsync(
                    userId,
                    [UserRoles.ACCOUNTANT],
                    "Bạn nhận được 1 thông báo mới",
                    "Có phiếu xuất kho mới vui lòng tạo hóa đơn cho phiếu xuất kho",
                    Core.Domain.Enums.NotificationType.Message
                    );

                return new ServiceResult<object>
                {
                    StatusCode = 201,
                    Message = "Tạo phiếu xuất kho thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");

                await _unitOfWork.RollbackTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<object>> DeleteAsync(int ginId, string userId)
        {
            try
            {
                var goodsIssueNote = await _unitOfWork.GoodsIssueNote.Query()
                    .Include(g => g.GoodsIssueNoteDetails)
                    .FirstOrDefaultAsync(g => g.Id == ginId);

                if (goodsIssueNote == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Phiếu xuất kho không tồn tại"
                    };

                if (goodsIssueNote.CreateBy != userId)
                    return new ServiceResult<object>
                    {
                        StatusCode = 403,
                        Message = "Bạn không có quyền xóa phiếu xuất này"
                    };

                if (goodsIssueNote.Status == Core.Domain.Enums.GoodsIssueNoteStatus.Sent)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Phiếu xuất đã được gửi không thể xóa"
                    };

                await _unitOfWork.BeginTransactionAsync();

                _unitOfWork.GoodsIssueNote.Remove(goodsIssueNote);

                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Xóa thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");

                await _unitOfWork.RollbackTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<object>> DetailsAsync(int ginId, string userId)
        {
            try
            {
                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId)
                    ?? throw new Exception("Không tìm thấy người dùng");

                var isWarehouseStaff = await _unitOfWork.Users.UserManager.IsInRoleAsync(user, UserRoles.WAREHOUSE_STAFF);

                var goodsIssueNote = await _unitOfWork.GoodsIssueNote.Query()
                    .AsNoTracking()
                    .Include(g => g.WarehouseStaff)
                    .Include(g => g.GoodsIssueNoteDetails)
                        .ThenInclude(d => d.LotProduct)
                            .ThenInclude(lp => lp.Product)
                    .FirstOrDefaultAsync(g => g.Id == ginId);

                if (goodsIssueNote == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy phiếu xuất kho"
                    };

                if (isWarehouseStaff)
                {
                    if (goodsIssueNote.CreateBy != user.Id)
                        return new ServiceResult<object>
                        {
                            StatusCode = 403,
                            Message = "Bạn không có quyền xem phiếu xuất này"
                        };
                }
                else
                {
                    if (goodsIssueNote.Status == Core.Domain.Enums.GoodsIssueNoteStatus.Draft)
                        return new ServiceResult<object>
                        {
                            StatusCode = 404,
                            Message = "Phiếu xuất chưa được gửi không thể xem"
                        };
                }

                var result = _mapper.Map<GoodsIssueNoteWithDetailsDTO>(goodsIssueNote);

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Data = result,
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
            };
        }

        public async Task<ServiceResult<object>> ListAsync(string userId)
        {
            try
            {
                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId)
                    ?? throw new Exception("Không tìm thấy người dùng");

                var isWarehouseStaff = await _unitOfWork.Users.UserManager.IsInRoleAsync(user, UserRoles.WAREHOUSE_STAFF);

                var query = _unitOfWork.GoodsIssueNote.Query()
                    .AsNoTracking()
                    .Include(g => g.WarehouseStaff)
                    .AsQueryable();

                if (isWarehouseStaff)
                    query = query.Where(g => g.CreateBy == user.Id);
                else
                    query = query.Where(g => g.Status == Core.Domain.Enums.GoodsIssueNoteStatus.Sent);

                var list = await query.ToListAsync();

                var result = _mapper.Map<List<GoodsIssueNoteListDTO>>(list);

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Data = result,
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

        public async Task<ServiceResult<object>> SendAsync(int ginId, string userId)
        {
            try
            {
                var goodsIssueNote = await _unitOfWork.GoodsIssueNote.Query()
                    .FirstOrDefaultAsync(g => g.Id == ginId);

                if (goodsIssueNote == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Phiếu xuất kho không tồn tại"
                    };

                if (goodsIssueNote.CreateBy != userId)
                    return new ServiceResult<object>
                    {
                        StatusCode = 403,
                        Message = "Bạn không có quyền gửi phiếu xuất này"
                    };

                if (goodsIssueNote.Status == Core.Domain.Enums.GoodsIssueNoteStatus.Sent)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Phiếu xuất đã được gửi"
                    };

                goodsIssueNote.ExportedAt = DateTime.Now;

                goodsIssueNote.Status = Core.Domain.Enums.GoodsIssueNoteStatus.Sent;

                _unitOfWork.GoodsIssueNote.Update(goodsIssueNote);

                await _unitOfWork.CommitAsync();

                await _notificationService.SendNotificationToRolesAsync(
                    userId,
                    [UserRoles.ACCOUNTANT],
                    "Bạn nhận được 1 thông báo mới",
                    "Có phiếu xuất kho mới vui lòng tạo hóa đơn cho phiếu xuất kho",
                    Core.Domain.Enums.NotificationType.Message
                    );

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Gửi phiếu xuất thành công"
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

        public async Task<ServiceResult<object>> UpdateAsync(UpdateGoodsIssueNoteDTO dto, string userId)
        {
            try
            {
                var goodsIssueNote = await _unitOfWork.GoodsIssueNote.Query()
                    .FirstOrDefaultAsync(g => g.Id == dto.GoodsIssueNoteId);

                if (goodsIssueNote == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Phiếu xuất kho không tồn tại"
                    };

                if (goodsIssueNote.CreateBy != userId)
                    return new ServiceResult<object>
                    {
                        StatusCode = 403,
                        Message = "Bạn không có quyền cập nhập phiếu xuất này"
                    };

                if (goodsIssueNote.Status == Core.Domain.Enums.GoodsIssueNoteStatus.Sent)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Phiếu xuất đã được gửi không thể cập nhập"
                    };

                if (!string.IsNullOrEmpty(dto.Note))
                    goodsIssueNote.Note = dto.Note;

                _unitOfWork.GoodsIssueNote.Update(goodsIssueNote);

                await _unitOfWork.CommitAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Cập nhật thành công"
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

        private static string GenerateGoodsIssueNoteCode()
        {
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return $"GIN-{randomPart}";
        }

        public async Task<ServiceResult<object>> WarningAsync()
        {
            try
            {
                var today = DateTime.Today;

                var query = await _unitOfWork.StockExportOrder.Query()
                    .Where(s => s.Status == Core.Domain.Enums.StockExportOrderStatus.Sent && s.DueDate < today.AddDays(3))
                    .ToListAsync();

                var result = query.Select(s => new
                {
                    s.StockExportOrderCode,
                    s.DueDate,
                });

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
    }
}

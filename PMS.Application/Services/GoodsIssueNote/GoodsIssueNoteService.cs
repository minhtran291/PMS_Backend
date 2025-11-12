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

                await _unitOfWork.BeginTransactionAsync();

                var gin = new Core.Domain.Entities.GoodsIssueNote
                {
                    StockExportOrderId = seo.Id,
                    CreateBy = userId,
                    CreateAt = DateTime.Now,
                    DeliveryDate = seo.DueDate,
                    Note = dto.Note,
                    Status = Core.Domain.Enums.GoodsIssueNoteStatus.Draft,
                    GoodsIssueNoteDetails = seo.StockExportOrderDetails.Select(d => new GoodsIssueNoteDetails
                    {
                        LotId = d.LotId,
                        Quantity = d.Quantity,
                    }).ToList(),
                };

                await _unitOfWork.GoodsIssueNote.AddAsync(gin);

                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();

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

                if(goodsIssueNote == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy phiếu xuất kho"
                    };

                if (isWarehouseStaff)
                {
                    if(goodsIssueNote.CreateBy != user.Id)
                        return new ServiceResult<object>
                        {
                            StatusCode = 403,
                            Message = "Bạn không có quyền xem phiếu xuất này"
                        };
                }
                else
                {
                    if(goodsIssueNote.Status == Core.Domain.Enums.GoodsIssueNoteStatus.Draft)
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
            catch(Exception ex)
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

                if(goodsIssueNote.Status == Core.Domain.Enums.GoodsIssueNoteStatus.Sent)
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
    }
}

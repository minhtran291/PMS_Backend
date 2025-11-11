using AutoMapper;
using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.StockExportOrder;
using PMS.Application.Services.Base;
using PMS.Application.Services.Notification;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using PMS.Data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.StockExportOrder
{
    public class StockExportOrderService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<StockExportOrderService> logger,
        INotificationService notificationService) : Service(unitOfWork, mapper), IStockExportOderService
    {
        private readonly ILogger<StockExportOrderService> _logger = logger;
        private readonly INotificationService _notificationService = notificationService;

        public async Task<ServiceResult<object>> CreateAsync(StockExportOrderDTO dto, string userId)
        {
            try
            {
                var salesOrder = await _unitOfWork.SalesOrder.Query()
                    .Include(so => so.SalesOrderDetails)
                    .FirstOrDefaultAsync(so => so.SalesOrderId == dto.SalesOrderId);

                if (salesOrder == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy đơn hàng mua"
                    };

                if (salesOrder.IsDeposited == false)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Đơn hàng chưa thanh toán cọc"
                    };

                if (dto.DueDate < DateTime.Now.Date)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Thời hạn yêu cầu xuất kho không được nhỏ hơn hôm nay"
                    };

                if (dto.Details.Count == 0 || dto.Details == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Chi tiết lệnh yêu cầu xuất kho không được để chống"
                    };

                if (dto.Details.Any(d => d.Quantity <= 0))
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Chi tiết xuất kho không được có số lượng nhỏ hơn hoặc bằng 0."
                    };

                //if(!dto.Details.Any(d => d.Quantity > 0))
                //    return new ServiceResult<object>
                //    {
                //        StatusCode = 400,
                //        Message = "Không có chi tiết nào có số lượng lớn hơn 0. Không thể tạo lệnh xuất kho."
                //    };

                var lotIdDetails = dto.Details.Select(d => d.LotId).ToList();

                if (lotIdDetails.Distinct().Count() != dto.Details.Count)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Có lô bị trùng lặp"
                    };

                var salesOrderDetails = salesOrder.SalesOrderDetails
                    .Where(d => lotIdDetails.Contains(d.LotId))
                    .ToList();

                if (salesOrderDetails.Count != lotIdDetails.Count)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Có lô không tồn tại hoặc không thuộc phạm vi của đơn hàng"
                    };

                //var previousExports = await _unitOfWork.StockExportOrder.Query()
                //    .Include(seo => seo.StockExportOrderDetails)
                //    .Where(seo => seo.SalesOrderId == dto.SalesOrderId
                //                  && seo.Status != StockExportOrderStatus.Draft)
                //    .ToListAsync();

                //if (previousExports.Any())
                //{
                //    var exportedQuantities = previousExports
                //        .SelectMany(seo => seo.StockExportOrderDetails)
                //        .GroupBy(d => d.LotId)
                //        .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                //    foreach (var detail in dto.Details)
                //    {
                //        var lotId = detail.LotId;
                //        var orderDetail = salesOrder.SalesOrderDetails.FirstOrDefault(d => d.LotId == lotId);

                //        if (orderDetail == null)
                //            return new ServiceResult<object>
                //            {
                //                StatusCode = 400,
                //                Message = $"Lô {lotId} không thuộc đơn hàng."
                //            };

                //        var alreadyExported = exportedQuantities.ContainsKey(lotId) ? exportedQuantities[lotId] : 0;

                //        var availableToExport = orderDetail.Quantity - alreadyExported;

                //        if (detail.Quantity > availableToExport)
                //            return new ServiceResult<object>
                //            {
                //                StatusCode = 400,
                //                Message = availableToExport <= 0
                //                    ? $"Lô {lotId} đã được xuất hết, không thể xuất thêm."
                //                    : $"Lô {lotId} chỉ còn được phép xuất tối đa {availableToExport} sản phẩm.",
                //            };
                //    }
                //}
                //else
                //{
                //    foreach (var detail in dto.Details)
                //    {
                //        var lotId = detail.LotId;
                //        var orderDetail = salesOrder.SalesOrderDetails.FirstOrDefault(d => d.LotId == lotId);

                //        if (orderDetail == null)
                //            return new ServiceResult<object>
                //            {
                //                StatusCode = 400,
                //                Message = $"Lô {lotId} không thuộc đơn hàng."
                //            };

                //        if (detail.Quantity > orderDetail.Quantity)
                //            return new ServiceResult<object>
                //            {
                //                StatusCode = 400,
                //                Message = $"Số lượng yêu cầu xuất lô {lotId} vượt quá số lượng đã lên trong đơn."
                //            };
                //    }
                //}

                foreach (var detail in dto.Details)
                {
                    var lotId = detail.LotId;
                    var orderDetail = salesOrder.SalesOrderDetails.FirstOrDefault(d => d.LotId == lotId);

                    if (orderDetail == null)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"Lô {lotId} không thuộc đơn hàng."
                        };

                    if (detail.Quantity > orderDetail.Quantity)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"Số lượng yêu cầu xuất lô {lotId} vượt quá số lượng đã lên trong đơn."
                        };
                }

                //var exportQuantityValidation = await ValidateExportQuantity(dto.Details, salesOrder);
                //if (exportQuantityValidation != null)
                //    return exportQuantityValidation;

                await _unitOfWork.BeginTransactionAsync();

                var newExport = new Core.Domain.Entities.StockExportOrder
                {
                    SalesOrderId = dto.SalesOrderId,
                    CreateBy = userId,
                    DueDate = dto.DueDate,
                    Status = StockExportOrderStatus.Draft,
                    StockExportOrderDetails = dto.Details.Select(d => new StockExportOrderDetails
                    {
                        LotId = d.LotId,
                        Quantity = d.Quantity
                    }).ToList()
                };

                await _unitOfWork.StockExportOrder.AddAsync(newExport);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 201,
                    Message = "Tạo lệnh yêu cầu xuất kho thành công"
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

        public async Task<ServiceResult<object>> DetailsAsync(int seoId, string userId)
        {
            try
            {
                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId)
                    ?? throw new Exception("Loi khong tim thay user");

                var isSales = await _unitOfWork.Users.UserManager.IsInRoleAsync(user, UserRoles.SALES_STAFF);

                var stockExportOrder = await _unitOfWork.StockExportOrder.Query()
                    .AsNoTracking()
                    .Include(seo => seo.StockExportOrderDetails)
                        .ThenInclude(d => d.LotProduct)
                            .ThenInclude(lp => lp.Product)
                    .FirstOrDefaultAsync(seo => seo.Id == seoId);

                if(stockExportOrder == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy lệnh yêu cầu xuất kho"
                    };

                if (isSales)
                {
                    if(stockExportOrder.CreateBy != user.Id)
                        return new ServiceResult<object>
                        {
                            StatusCode = 403,
                            Message = "Bạn không có quyền xem lệnh yêu cầu xuất kho này"
                        };
                }
                else
                {
                    if(stockExportOrder.Status != StockExportOrderStatus.Sent)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = "Lệnh chưa được gửi bạn không thể xem"
                        };
                }

                var result = _mapper.Map<ViewModelDetails>(stockExportOrder);

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

        public async Task<ServiceResult<object>> ListAsync(string userId)
        {
            try
            {
                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId)
                    ?? throw new Exception("Loi khong tim thay user");

                var isSales = await _unitOfWork.Users.UserManager.IsInRoleAsync(user, UserRoles.SALES_STAFF);

                var query = _unitOfWork.StockExportOrder.Query()
                    .AsNoTracking()
                    .Include(s => s.SalesOrder)
                    .Include(s => s.SalesStaff)
                    .AsQueryable();

                if (isSales)
                    query = query.Where(seo => seo.CreateBy == userId);
                else
                    query = query.Where(seo => seo.Status == StockExportOrderStatus.Sent);

                var list = await query.ToListAsync();

                var result = _mapper.Map<List<ListStockExportOrderDTO>>(list);

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

        public async Task<ServiceResult<object>> SendAsync(int seoId, string userId)
        {
            try
            {
                var stockExportOrder = await _unitOfWork.StockExportOrder.Query()
                .Include(seo => seo.StockExportOrderDetails)
                .Include(seo => seo.SalesOrder)
                    .ThenInclude(so => so.SalesOrderDetails)
                .FirstOrDefaultAsync(seo => seo.Id == seoId);

                if (stockExportOrder == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy lệnh yêu cầu xuất kho"
                    };

                if (stockExportOrder.CreateBy != userId)
                    return new ServiceResult<object>
                    {
                        StatusCode = 403,
                        Message = "Bạn không có quyền gửi lệnh yêu cầu xuất kho này"
                    };

                if (stockExportOrder.DueDate.Date < DateTime.Now.Date)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Thời hạn yêu cầu xuất kho không được nhỏ hơn hôm nay"
                    };

                // tim tat ca cac lenh xuat trc day ma trang thai ko phai draft
                var previousExports = await _unitOfWork.StockExportOrder.Query()
                    .Include(seo => seo.StockExportOrderDetails)
                    .Where(seo => seo.SalesOrderId == stockExportOrder.SalesOrder.SalesOrderId
                                  && seo.Status != StockExportOrderStatus.Draft)
                    .ToListAsync();

                // neu co
                if (previousExports.Any())
                {
                    var exportedQuantities = previousExports
                        .SelectMany(seo => seo.StockExportOrderDetails)
                        .GroupBy(d => d.LotId)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                    foreach (var detail in stockExportOrder.StockExportOrderDetails)
                    {
                        var lotId = detail.LotId;
                        var orderDetail = stockExportOrder.SalesOrder.SalesOrderDetails.FirstOrDefault(d => d.LotId == lotId);

                        if (orderDetail == null)
                            return new ServiceResult<object>
                            {
                                StatusCode = 400,
                                Message = $"Lô {lotId} không thuộc đơn hàng."
                            };

                        var alreadyExported = exportedQuantities.ContainsKey(lotId) ? exportedQuantities[lotId] : 0;

                        var availableToExport = orderDetail.Quantity - alreadyExported;

                        if (detail.Quantity > availableToExport)
                            return new ServiceResult<object>
                            {
                                StatusCode = 400,
                                Message = availableToExport <= 0
                                                    ? $"Lô {lotId} đã được xuất hết, không thể xuất thêm."
                                                    : $"Lô {lotId} chỉ còn được phép xuất tối đa {availableToExport} sản phẩm.",
                            };
                    }
                }

                stockExportOrder.RequestDate = DateTime.Now;
                stockExportOrder.Status = StockExportOrderStatus.Sent;

                _unitOfWork.StockExportOrder.Update(stockExportOrder);

                await _unitOfWork.CommitAsync();

                await _notificationService.SendNotificationToRolesAsync(
                    userId,
                    ["WAREHOUSE_STAFF"],
                    "Bạn nhận được 1 thông báo mới",
                    "Lệnh yêu cầu xuất kho",
                    NotificationType.Message
                    );

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Gửi lệnh yêu cầu xuất kho thành công"
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

        private async Task<ServiceResult<object>?> ValidateExportQuantity(List<StockExportOrderDetailsDTO> details, Core.Domain.Entities.SalesOrder salesOrder)
        {
            var previousExports = await _unitOfWork.StockExportOrder.Query()
                .Include(seo => seo.StockExportOrderDetails)
                .Where(seo => seo.SalesOrderId == salesOrder.SalesOrderId
                              && seo.Status != StockExportOrderStatus.Draft)
                .ToListAsync();

            if (previousExports.Any())
            {
                var exportedQuantities = previousExports
                    .SelectMany(seo => seo.StockExportOrderDetails)
                    .GroupBy(d => d.LotId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                foreach (var detail in details)
                {
                    var lotId = detail.LotId;
                    var orderDetail = salesOrder.SalesOrderDetails.FirstOrDefault(d => d.LotId == lotId);

                    if (orderDetail == null)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"Lô {lotId} không thuộc đơn hàng."
                        };

                    var alreadyExported = exportedQuantities.ContainsKey(lotId) ? exportedQuantities[lotId] : 0;

                    var availableToExport = orderDetail.Quantity - alreadyExported;

                    if (detail.Quantity > availableToExport)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = availableToExport <= 0
                                ? $"Lô {lotId} đã được xuất hết, không thể xuất thêm."
                                : $"Lô {lotId} chỉ còn được phép xuất tối đa {availableToExport} sản phẩm.",
                        };
                }
            }
            else
            {
                foreach (var detail in details)
                {
                    var lotId = detail.LotId;
                    var orderDetail = salesOrder.SalesOrderDetails.FirstOrDefault(d => d.LotId == lotId);

                    if (orderDetail == null)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"Lô {lotId} không thuộc đơn hàng."
                        };

                    if (detail.Quantity > orderDetail.Quantity)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"Số lượng yêu cầu xuất lô {lotId} vượt quá số lượng đã lên trong đơn."
                        };
                }
            }

            return null;
        }
    }
}

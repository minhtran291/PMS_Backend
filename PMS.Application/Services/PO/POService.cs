using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PMS.Application.DTOs.PO;
using PMS.Application.Services.Base;
using PMS.Application.Services.PO;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.POService
{
    public class POService(IUnitOfWork unitOfWork, IMapper mapper)
        : Service(unitOfWork, mapper), IPOService
    {
        public async Task<ServiceResult<IEnumerable<POViewDTO>>> GetAllPOAsync()
        {
            var poList = await _unitOfWork.PurchasingOrder.Query()
                .Include(p => p.User)
                .ToListAsync();

            var userList = await _unitOfWork.Users.Query()
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();

            var result = poList.Select(p =>
            {
                var paymentUser = userList.FirstOrDefault(u => u.Id == p.PaymentBy);
                return new POViewDTO
                {
                    POID = p.POID,
                    OrderDate = p.OrderDate,
                    QID = p.QID,
                    Total = p.Total,
                    Status = p.Status,
                    Deposit = p.Deposit,
                    Debt = p.Debt,
                    PaymentDate = p.PaymentDate,
                    UserName = p.User?.UserName ?? "Unknown",
                    PaymentBy = paymentUser?.UserName ?? "Unknown"
                };
            }).ToList();

            return new ServiceResult<IEnumerable<POViewDTO>>
            {
                Data = result,
                StatusCode = 200,
                Message = "Thành công"
            };
        }

        public async Task<ServiceResult<POPaidViewDTO>> DepositedPOAsync(string userId, int poid, POUpdateDTO pOUpdateDTO)
        {
            try
            {
                var existingPO = await _unitOfWork.PurchasingOrder.Query()
                   
                    .FirstOrDefaultAsync(po => po.POID == poid);

                if (existingPO == null)
                {
                    return new ServiceResult<POPaidViewDTO>
                    {
                        StatusCode = 404,
                        Message = $"Không tìm thấy đơn hàng với POID = {poid}",
                        Data = null
                    };
                }

                if (pOUpdateDTO.paid > existingPO.Total)
                {
                    return new ServiceResult<POPaidViewDTO>
                    {
                        StatusCode = 400,
                        Message = "Thanh toán vượt quá tổng giá trị đơn hàng",
                        Data = null
                    };
                }

                existingPO.Status = Core.Domain.Enums.PurchasingOrderStatus.deposited;
                existingPO.Deposit = pOUpdateDTO.paid;
                existingPO.Debt = existingPO.Total - pOUpdateDTO.paid;
                existingPO.PaymentDate = DateTime.Now;
                existingPO.PaymentBy = userId;

                

                _unitOfWork.PurchasingOrder.Update(existingPO);
                await _unitOfWork.CommitAsync();
                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(existingPO.PaymentBy);
                if (user == null)
                {
                    throw new Exception("Lỗi khi ghi nhận tiền gửi");
                }
                var paymentName = user.UserName;

                var resultDto = new POPaidViewDTO
                {                   
                    PaymentBy = paymentName,
                    PaymentDate = existingPO.PaymentDate,
                    Status = existingPO.Status,
                    Debt = existingPO.Debt,  
                };

                return new ServiceResult<POPaidViewDTO>
                {
                    StatusCode = 200,
                    Message = "Xác nhận tiền cọc thành công.",
                    Data = resultDto
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<POPaidViewDTO>
                {
                    StatusCode = 500,
                    Message = $"Lỗi khi cập nhật đơn hàng: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<POViewDTO>> ViewDetailPObyID(int poid)
        {
            try
            {
                var expo = await _unitOfWork.PurchasingOrder.Query()
                    .Include(po => po.User)
                    .Include(po => po.PurchasingOrderDetails)
                    .FirstOrDefaultAsync(po => po.POID == poid);

                if (expo == null)
                {
                    return new ServiceResult<POViewDTO>
                    {
                        Data = null,
                        StatusCode = 404,
                        Message = "Không tìm thấy PO với ID này."
                    };
                }

                var paymentUserName = await _unitOfWork.Users.Query()
                                        .Where(u => u.Id == expo.PaymentBy)
                                        .Select(u => u.UserName)
                                        .FirstOrDefaultAsync();

                var createdByName = expo.User?.UserName
                                    ?? await _unitOfWork.Users.Query()
                                        .Where(u => u.Id == expo.UserId)
                                        .Select(u => u.UserName)
                                        .FirstOrDefaultAsync();
                var dto = new POViewDTO
                {
                    POID = expo.POID,
                    Total = expo.Total,
                    OrderDate = expo.OrderDate,
                    QID = expo.QID,
                    Debt = expo.Debt,
                    Deposit = expo.Deposit,
                    PaymentBy = paymentUserName ?? "Unknown",
                    UserName = createdByName,
                    PaymentDate = expo.PaymentDate,
                    Status = expo.Status,
                    Details = expo.PurchasingOrderDetails?.Select(d => new PurchasingOrderDetail
                    {

                        ProductName = d.ProductName,
                        DVT = d.DVT,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        UnitPriceTotal = d.UnitPriceTotal,
                        Description = d.Description,
                        ExpiredDate = d.ExpiredDate,
                        PODID = d.PODID,
                        ProductID = d.ProductID,

                    }).ToList()
                };

                return new ServiceResult<POViewDTO>
                {
                    Data = dto,
                    StatusCode = 200,
                    Message = "Lấy chi tiết PO thành công."
                };

            }
            catch (Exception ex)
            {


                return new ServiceResult<POViewDTO>
                {
                    Data = null,
                    StatusCode = 500,
                    Message = "Lỗi hệ thống"
                };
            }
        }

        public async Task<ServiceResult<POPaidViewDTO>> DebtAccountantPOAsync(string userId, int poid, POUpdateDTO pOUpdateDTO)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingPO = await _unitOfWork.PurchasingOrder.Query()
                    .FirstOrDefaultAsync(po => po.POID == poid);

                if (existingPO == null)
                {
                    return new ServiceResult<POPaidViewDTO>
                    {
                        StatusCode = 404,
                        Message = $"Không tìm thấy đơn hàng với POID = {poid}",
                        Data = null
                    };
                }

                
                if (pOUpdateDTO.paid > existingPO.Debt)
                {
                    return new ServiceResult<POPaidViewDTO>
                    {
                        StatusCode = 400,
                        Message = "Thanh toán vượt quá số nợ còn lại.",
                        Data = null
                    };
                }

                
                existingPO.Deposit += pOUpdateDTO.paid;
                existingPO.Debt = existingPO.Total - existingPO.Deposit;

               
                if (existingPO.Debt == 0)
                {
                    existingPO.Status = PurchasingOrderStatus.compeleted;
                }
                else
                {
                    existingPO.Status = PurchasingOrderStatus.paid;
                }

                existingPO.PaymentDate = DateTime.Now;
                existingPO.PaymentBy = userId;

                _unitOfWork.PurchasingOrder.Update(existingPO);
                await _unitOfWork.CommitAsync();

               
                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(existingPO.PaymentBy);
                if (user == null)
                {
                    throw new Exception("Không tìm thấy thông tin người xác nhận thanh toán.");
                }

                var paymentName = user.UserName;

                
                var resultDto = new POPaidViewDTO
                {
                    PaymentBy = paymentName,
                    PaymentDate = existingPO.PaymentDate,
                    Status = existingPO.Status,
                    Debt = existingPO.Debt
                };

                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<POPaidViewDTO>
                {
                    StatusCode = 200,
                    Message = "Cập nhật thanh toán thành công.",
                    Data = resultDto
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                return new ServiceResult<POPaidViewDTO>
                {
                    StatusCode = 500,
                    Message = $"Lỗi khi cập nhật đơn hàng: {ex.Message}",
                    Data = null
                };
            }
        }
        
        public async Task<ServiceResult<bool>> ChangeStatusAsync(int poid, PurchasingOrderStatus newStatus)
        {
            try
            {
                var existingPO = await _unitOfWork.PurchasingOrder
                    .Query()
                    .FirstOrDefaultAsync(po => po.POID == poid);

                if (existingPO == null)
                {
                    return ServiceResult<bool>.Fail($"Không tìm thấy đơn hàng với ID: {poid}", 404);
                }

                if (existingPO.Status == newStatus)
                {
                    return ServiceResult<bool>.Fail("Đơn hàng đã ở trạng thái này rồi", 400);
                }

                if (!IsValidStatusTransition(existingPO.Status, newStatus))
                {
                    return ServiceResult<bool>.Fail(
                        $"Không thể chuyển trạng thái từ {existingPO.Status} sang {newStatus}", 400);
                }


                existingPO.Status = newStatus;


                if (newStatus == PurchasingOrderStatus.paid)
                {
                    existingPO.PaymentDate = DateTime.Now;
                    existingPO.Debt = 0;
                }

                _unitOfWork.PurchasingOrder.Update(existingPO);
                await _unitOfWork.CommitAsync();

                return ServiceResult<bool>.SuccessResult(true,
                    $"Cập nhật trạng thái đơn hàng {poid} thành công: {newStatus}", 200);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Lỗi khi cập nhật trạng thái: {ex.Message}", 500);
            }
        }

        //thứ tự trạng thái
        private bool IsValidStatusTransition(PurchasingOrderStatus current, PurchasingOrderStatus next)
        {
            return (current, next) switch
            {
                (PurchasingOrderStatus.draft, PurchasingOrderStatus.sent) => true,
                (PurchasingOrderStatus.sent, PurchasingOrderStatus.approved) => true,
                (PurchasingOrderStatus.sent, PurchasingOrderStatus.rejected) => true,
                (PurchasingOrderStatus.approved, PurchasingOrderStatus.deposited) => true,
                (PurchasingOrderStatus.deposited, PurchasingOrderStatus.paid) => true,
                (PurchasingOrderStatus.paid, PurchasingOrderStatus.compeleted) => true,
                _ => false
            };
        }

    }
}

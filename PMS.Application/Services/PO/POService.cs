using System.Drawing;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
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

        public async Task<byte[]> GeneratePOPaymentExcelAsync(int poid)
        {
            var po = await _unitOfWork.PurchasingOrder.Query()
                .Include(p => p.Quotations)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.POID == poid);

            if (po == null)
                throw new Exception($"Không tìm thấy đơn hàng với POID = {poid}");

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Thanh toán đơn hàng");

            ws.Cells.Style.Font.Name = "Arial";
            ws.Cells.Style.Font.Size = 11;
            ws.Cells.Style.WrapText = false;
            ws.DefaultRowHeight = 18;

            //
            ws.Cells[1, 1, 1, 6].Merge = true;
            ws.Cells[1, 1].Value = "BÁO CÁO THANH TOÁN ĐƠN HÀNG (PURCHASING ORDER PAYMENT)";
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 16;
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 102, 204));
            ws.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
            ws.Row(1).Height = 28;

            int row = 3;
            //
            ws.Cells[row, 1].Value = "Mã đơn hàng (POID):";
            ws.Cells[row, 2].Value = po.POID;
            ws.Cells[row, 1].Style.Font.Bold = true;

            ws.Cells[row, 4].Value = "Trạng thái:";
            ws.Cells[row, 5, row, 6].Merge = true;
            ws.Cells[row, 5].Value = po.Status.ToString();
            row++;

            ws.Cells[row, 1].Value = "Từ báo giá:";
            ws.Cells[row, 2, row, 3].Merge = true;
            ws.Cells[row, 2].Value = po.QID;
            ws.Cells[row, 1].Style.Font.Bold = true;

            ws.Cells[row, 4].Value = "Người tạo đơn:";
            ws.Cells[row, 5, row, 6].Merge = true;
            ws.Cells[row, 5].Value = po.User?.UserName ?? "—";
            row++;

            ws.Cells[row, 1].Value = "Tổng giá trị:";
            ws.Cells[row, 2].Value = po.Total;
            ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, 1].Style.Font.Bold = true;

            ws.Cells[row, 4].Value = "Tiền gửi (Paid):";
            ws.Cells[row, 5].Value = po.Deposit;
            ws.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";
            row++;

            ws.Cells[row, 1].Value = "Công nợ còn lại:";
            ws.Cells[row, 2].Value = po.Debt;
            ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, 1].Style.Font.Bold = true;

            ws.Cells[row, 4].Value = "Ngày thanh toán:";
            ws.Cells[row, 5, row, 6].Merge = true;
            if (po.PaymentDate != default)
            {
                ws.Cells[row, 5].Value = po.PaymentDate;
                ws.Cells[row, 5].Style.Numberformat.Format = "dd/MM/yyyy HH:mm";
            }
            else
            {
                ws.Cells[row, 5].Value = "—";
            }
            row++;

            var paymentUser = po.PaymentBy != null
                ? await _unitOfWork.Users.UserManager.FindByIdAsync(po.PaymentBy)
                : null;
            string paymentName = paymentUser?.UserName ?? "—";

            ws.Cells[row, 1].Value = "Người xác nhận thanh toán:";
            ws.Cells[row, 2, row, 3].Merge = true;
            ws.Cells[row, 2].Value = paymentName;
            ws.Cells[row, 1].Style.Font.Bold = true;

            row += 2;

            // chi tiet
            ws.Cells[row, 1, row, 6].Merge = true;
            ws.Cells[row, 1].Value = "CHI TIẾT THANH TOÁN";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 13;
            ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            row++;

            string[] headers = { "STT", "Trạng thái", "Tổng giá trị", "Tiền đã thanh toán", "Công nợ còn lại", "Ngày cập nhật" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[row, i + 1].Value = headers[i];
                ws.Cells[row, i + 1].Style.Font.Bold = true;
                ws.Cells[row, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 153, 0));
                ws.Cells[row, i + 1].Style.Font.Color.SetColor(Color.White);
                ws.Cells[row, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            row++;

            ws.Cells[row, 1].Value = 1;
            ws.Cells[row, 2].Value = po.Status.ToString();
            ws.Cells[row, 3].Value = po.Total;
            ws.Cells[row, 3].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, 4].Value = po.Deposit;
            ws.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, 5].Value = po.Debt;
            ws.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";
            if (po.PaymentDate != default)
            {
                ws.Cells[row, 6].Value = po.PaymentDate;
                ws.Cells[row, 6].Style.Numberformat.Format = "dd/MM/yyyy HH:mm";
            }
            else
            {
                ws.Cells[row, 6].Value = "—";
            }

            var tableRange = ws.Cells[row - 1, 1, row, 6];
            tableRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            tableRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            tableRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            tableRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            tableRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            row += 3;

            //ghi chu
            ws.Cells[row, 1, row, 6].Merge = true;
            ws.Cells[row, 1].Value = "(Tệp này được tạo tự động từ hệ thống kế toán – vui lòng không chỉnh sửa thủ công)";
            ws.Cells[row, 1].Style.Font.Italic = true;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[row, 1].Style.Font.Size = 9;
            row += 2;


                        // note
            ws.Cells[row, 1, row, 6].Merge = true;
            ws.Cells[row, 1].Value = "GHI CHÚ (NOTES)";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 13;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            row++;


            string[] noteLines =
            {
    $"Tôi, {paymentName}, với tư cách là bên mua, xác nhận đã thực hiện thanh toán đúng và đầy đủ số tiền theo thỏa thuận giữa hai bên.",
    "Việc thanh toán này được thực hiện hoàn toàn tự nguyện, minh bạch, không bị ép buộc, lừa dối hoặc chi phối dưới bất kỳ hình thức nào.",
    "Tôi hiểu và đồng ý rằng việc hoàn tất thanh toán đồng nghĩa với việc xác lập quyền sở hữu, nghĩa vụ nhận hàng hóa/dịch vụ theo hợp đồng đã ký kết.",
    "Tôi cam kết chịu hoàn toàn trách nhiệm trước pháp luật Việt Nam về tính trung thực, chính xác của thông tin và giao dịch nêu trên."
};

            foreach (var line in noteLines)
            {
                ws.Cells[row, 1, row, 6].Merge = true;
                ws.Cells[row, 1].Value = line;
                ws.Cells[row, 1].Style.WrapText = true;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Justify;
                ws.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                ws.Cells[row, 1].Style.Font.Italic = true;
                row++;
            }

            row += 2; 

            ws.Cells[row, 1, row, 6].Merge = true;
            ws.Cells[row, 1].Value = $"Tôi bên mua (A): ....................................................      Ngày ký: {DateTime.Now:dd/MM/yyyy}";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;


            // alige
            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            for (int i = 1; i <= 6; i++)
                ws.Column(i).Width = Math.Min(ws.Column(i).Width, 40);

            ws.View.ZoomScale = 100;

            return package.GetAsByteArray();
        }


    }
}

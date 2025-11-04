using System.Drawing;
using System.Threading.Tasks;
using AutoMapper;
using DinkToPdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PMS.Application.DTOs.PO;
using PMS.Application.Services.Base;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.PO;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.POService
{
    public class POService(IUnitOfWork unitOfWork, IMapper mapper, IWebHostEnvironment webHostEnvironment, IPdfService pdfService)
        : Service(unitOfWork, mapper), IPOService
    {
        public async Task<ServiceResult<IEnumerable<POViewDTO>>> GetAllPOAsync()
        {
            var poList = await _unitOfWork.PurchasingOrder.Query()
                .Include(p => p.User)
                .Include(p => p.Quotations)
                .ToListAsync();

            var userList = await _unitOfWork.Users.Query()
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();

            var result = new List<POViewDTO>();

            foreach (var p in poList)
            {
                var quotation = _unitOfWork.Quotation.Query().FirstOrDefault();
                Supplier? sup = null;

                if (quotation != null)
                {
                   
                    sup = await _unitOfWork.Supplier.Query()
                        .FirstOrDefaultAsync(s => s.Id == quotation.SupplierID);
                }

                var paymentUser = userList.FirstOrDefault(u => u.Id == p.PaymentBy);

                result.Add(new POViewDTO
                {
                    POID = p.POID,
                    OrderDate = p.OrderDate,
                    QID = p.QID,
                    Total = p.Total,
                    Status = p.Status,
                    Deposit = p.Deposit,
                    Debt = p.Debt,
                    PaymentDate = p.PaymentDate,
                    UserName = p.User?.FullName ?? "Unknown",
                    PaymentBy = paymentUser?.FullName ?? "Unknown",
                    supplierName = sup?.Name ?? "Unknown"
                });
            }

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
                                        .Select(u => u.FullName)
                                        .FirstOrDefaultAsync();

                var createdByName = expo.User?.FullName
                                    ?? await _unitOfWork.Users.Query()
                                        .Where(u => u.Id == expo.UserId)
                                        .Select(u => u.FullName)
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

        public async Task<byte[]> GeneratePOPaymentPdfAsync(int poid)
        {
            var po = await _unitOfWork.PurchasingOrder.Query()
                .Include(p => p.Quotations)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.POID == poid);

            if (po == null)
                throw new Exception($"Không tìm thấy đơn hàng với POID = {poid}");

            var paymentUser = po.PaymentBy != null
                ? await _unitOfWork.Users.UserManager.FindByIdAsync(po.PaymentBy)
                : null;
            string paymentName = paymentUser?.UserName ?? "—";

            string logoPath = Path.Combine(webHostEnvironment.WebRootPath, "assets", "myESign.png");
            string sealPath = Path.Combine(webHostEnvironment.WebRootPath, "assets", "myEsignCompany.png");
            string backgroundPath = Path.Combine(webHostEnvironment.WebRootPath, "assets", "background.png");

            string logoBase64 = File.Exists(logoPath)
                ? $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(logoPath))}"
                : "";
            string sealBase64 = File.Exists(sealPath)
                ? $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(sealPath))}"
                : "";


            string html = $@"
        <html>
        <head>
            <meta charset='UTF-8'>
           <style>
            @page {{
                margin: 0;
            }}

            html, body {{
                width: 100%;
                height: 100%;
                margin: 0;
                padding: 0;
                font-family: Arial, sans-serif;
                font-size: 12pt;
                color: #333;
                position: relative;

                /* Giữ đúng tỷ lệ ảnh nền */
                background: url('file:///{backgroundPath.Replace("\\", "/")}') no-repeat center center ;
                background-size: cover; /* cover = fill hết mà không méo */
                background-attachment: local;
            }}

            /* Lớp phủ làm mờ nền */
            body::before {{
                content: """";
                position: absolute;
                top: 0; left: 0; right: 0; bottom: 0;
                background-color: rgba(255, 255, 255, 0.82); /* độ mờ */
                z-index: 0;
            }}

            /* Nội dung chính */
            .content {{
                position: relative;
                z-index: 1;
                padding: 40px 50px;
            }}

            h1 {{
                background-color: #0066CC;
                color: white;
                text-align: center;
                padding: 10px;
                font-size: 16pt;
                border-radius: 6px;
            }}

            table {{
                width: 100%;
                border-collapse: collapse;
                margin-top: 15px;
            }}

            td, th {{
                border: 1px solid #999;
                padding: 6px 8px;
                vertical-align: middle;
            }}

            th {{
                background-color: #009900;
                color: white;
                text-align: center;
            }}

            .section-title {{
                background-color: #d9d9d9;
                text-align: center;
                font-weight: bold;
                padding: 6px;
                font-size: 13pt;
                margin-top: 15px;
            }}

            .note {{
                font-style: italic;
                text-align: justify;
                margin: 10px 0;
            }}

            .signature {{
                margin-top: 40px;
                text-align: center;
            }}

            .small-note {{
                font-size: 9pt;
                text-align: center;
                color: #666;
                margin-top: 20px;
            }}
        </style>
        </head>
        <body>
   <div class='content'>

    <h1>BÁO CÁO THANH TOÁN ĐƠN HÀNG (PURCHASING ORDER PAYMENT)</h1>

    <table>
        <tr>
            <td><b>Mã đơn hàng (POID):</b></td><td>{po.POID}</td>
            <td><b>Trạng thái:</b></td><td>{po.Status}</td>
        </tr>
        <tr>
            <td><b>Từ báo giá:</b></td><td>{po.QID}</td>
            <td><b>Người tạo đơn:</b></td><td>{po.User?.UserName ?? "—"}</td>
        </tr>
        <tr>
            <td><b>Tổng giá trị:</b></td><td>{po.Total:N2}</td>
            <td><b>Tiền gửi (Paid):</b></td><td>{po.Deposit:N2}</td>
        </tr>
        <tr>
            <td><b>Công nợ còn lại:</b></td><td>{po.Debt:N2}</td>
            <td><b>Ngày thanh toán:</b></td><td>{(po.PaymentDate != default ? po.PaymentDate.ToString("dd/MM/yyyy HH:mm") : "—")}</td>
        </tr>
        <tr>
            <td><b>Người xác nhận thanh toán:</b></td><td colspan='3'>{paymentName}</td>
        </tr>
    </table>

    <div class='section-title'>CHI TIẾT THANH TOÁN</div>

    <table>
        <thead>
            <tr>
                <th>STT</th>
                <th>Trạng thái</th>
                <th>Tổng giá trị</th>
                <th>Tiền đã thanh toán</th>
                <th>Công nợ còn lại</th>
                <th>Ngày cập nhật</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>1</td>
                <td>{po.Status}</td>
                <td>{po.Total:N2}</td>
                <td>{po.Deposit:N2}</td>
                <td>{po.Debt:N2}</td>
                <td>{(po.PaymentDate != default ? po.PaymentDate.ToString("dd/MM/yyyy HH:mm") : "—")}</td>
            </tr>
        </tbody>
    </table>

    <p class='small-note'>(Tệp này được tạo tự động từ hệ thống kế toán – vui lòng không chỉnh sửa thủ công)</p>

    <div class='section-title'>GHI CHÚ (NOTES)</div>

    <p class='note'>Tôi, {paymentName}, với tư cách là bên mua, xác nhận đã thực hiện thanh toán đúng và đầy đủ số tiền theo thỏa thuận giữa hai bên.</p>
    <p class='note'>Việc thanh toán này được thực hiện hoàn toàn tự nguyện, minh bạch, không bị ép buộc, lừa dối hoặc chi phối dưới bất kỳ hình thức nào.</p>
    <p class='note'>Tôi hiểu và đồng ý rằng việc hoàn tất thanh toán đồng nghĩa với việc xác lập quyền sở hữu, nghĩa vụ nhận hàng hóa/dịch vụ theo hợp đồng đã ký kết.</p>
    <p class='note'>Tôi cam kết chịu hoàn toàn trách nhiệm trước hiến pháp và pháp luật nước CHXHCN Việt Nam về tính trung thực, chính xác của thông tin và giao dịch nêu trên.</p>

    <div class='signature'>
        <table>
            <tr style=""text-align: center;"">
                <td><b>Giám đốc DH (Chair man)</b></td>
                <td><b>Kế toán trưởng (Accountant)</b></td>
            </tr>
            <tr>
                <td style='height:120px'></td>
                <td style='text-align:center; position:relative;'>

                    {(string.IsNullOrEmpty(sealBase64) ? "" :
                                $"<img src='{sealBase64}' style='width:140px;height:auto;opacity:0.9;'/>")}
                    

                    {(string.IsNullOrEmpty(logoBase64) ?
                                "<br/><span style='color:red'>(Không có chữ ký điện tử)</span>" :
                                $"<img src='{logoBase64}' style='width:180px;height:auto;position:relative;margin-top:-200px;margin-left:-120px;z-index:2;'/>")}
                    
                    <div style='font-style:italic;color:gray;margin-top:-10px;'>
                         Nhân viên KÝ RÕ HỌ TÊN
                    </div>
                </td>
            </tr>
        </table>
    </div>

  </div>
</body>
</html>";

            var pdfBytes = pdfService.GeneratePdfFromHtml(html);
            return pdfBytes;
        }

    }
}

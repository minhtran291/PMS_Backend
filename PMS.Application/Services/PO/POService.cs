using System.Drawing;
using System.Threading.Tasks;
using AutoMapper;
using DinkToPdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PMS.Application.DTOs.GRN;
using PMS.Application.DTOs.PO;
using PMS.Application.Services.Base;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Notification;
using PMS.Application.Services.PO;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using PMS.Data.Migrations;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.POService
{
    public class POService(IUnitOfWork unitOfWork, IMapper mapper, IWebHostEnvironment webHostEnvironment, IPdfService pdfService, INotificationService notificationService)
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

        public async Task<ServiceResult<POViewDTO2>> ViewDetailPObyID2(int poid)
        {
            try
            {
                // Lấy PO cùng chi tiết
                var po = await _unitOfWork.PurchasingOrder.Query()
                    .Include(po => po.User)
                    .Include(po => po.PurchasingOrderDetails)
                    .FirstOrDefaultAsync(po => po.POID == poid);

                if (po == null)
                {
                    return new ServiceResult<POViewDTO2>
                    {
                        Data = null,
                        StatusCode = 404,
                        Message = "Không tìm thấy PO với ID này."
                    };
                }

                // Lấy tên người thanh toán
                var paymentUserName = await _unitOfWork.Users.Query()
                    .Where(u => u.Id == po.PaymentBy)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync();

                // Lấy tên người tạo PO
                var createdByName = po.User?.FullName
                    ?? await _unitOfWork.Users.Query()
                        .Where(u => u.Id == po.UserId)
                        .Select(u => u.FullName)
                        .FirstOrDefaultAsync();

                // Lấy danh sách GRNDetails đã nhập cho PO này
                var grnDetails = await _unitOfWork.GoodReceiptNoteDetail.Query()
                    .Include(d => d.GoodReceiptNote)
                    .Where(d => d.GoodReceiptNote.POID == poid)
                    .ToListAsync();

                // Build DTO chi tiết PO
                var dto = new POViewDTO2
                {
                    POID = po.POID,
                    Total = po.Total,
                    OrderDate = po.OrderDate,
                    QID = po.QID,
                    Debt = po.Debt,
                    Deposit = po.Deposit,
                    PaymentBy = paymentUserName ?? "Unknown",
                    UserName = createdByName,
                    PaymentDate = po.PaymentDate,
                    Status = po.Status,
                    Details = po.PurchasingOrderDetails?.Select(d =>
                    {
                        // Tính số lượng đã nhập trước đó
                        var totalReceived = grnDetails
                            .Where(g => g.ProductID == d.ProductID)
                            .Sum(g => g.Quantity);

                        var remainingQty = d.Quantity - totalReceived;

                        return new PMS.Application.DTOs.GRN.PODetailViewDTO
                        {
                            PODID = d.PODID,
                            ProductID = d.ProductID,
                            ProductName = d.ProductName,
                            DVT = d.DVT,
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice,
                            UnitPriceTotal = d.UnitPriceTotal,
                            Description = d.Description,
                            ExpiredDate = d.ExpiredDate,
                            RemainingQty = remainingQty > 0 ? remainingQty : 0,
                            Tax = d.Tax
                        };
                    }).ToList()
                };

                return new ServiceResult<POViewDTO2>
                {
                    Data = dto,
                    StatusCode = 200,
                    Message = "Lấy chi tiết PO thành công."
                };
            }
            catch (Exception)
            {
                return new ServiceResult<POViewDTO2>
                {
                    Data = null,
                    StatusCode = 500,
                    Message = "Lỗi hệ thống"
                };
            }
        }
        public async Task<ServiceResult<bool>> ChangeStatusAsync(string userId, int poid, PurchasingOrderStatus newStatus)
        {
            var senderUser = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
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

                await notificationService.SendNotificationToRolesAsync(
                userId,
                ["ACCOUNTANT"],
                "Yêu cầu Thanh toán",
                $"Đơn hàng {existingPO.POID} đã được chấp nhận, Nhân viên {senderUser.FullName} yêu cầu thanh toán cho đơn hàng ",
                Core.Domain.Enums.NotificationType.Message
                );

                await notificationService.SendNotificationToRolesAsync(
                userId,
                ["WAREHOUSE_STAFF"],
                "Yêu cầu nhập kho",
                $"Nhân viên {senderUser.FullName} yêu cầu Tạo phiếu nhập kho cho đơn hàng: {existingPO.POID} ",
                Core.Domain.Enums.NotificationType.Message
                );
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


        public async Task<ServiceResult<Dictionary<string, IEnumerable<POQuantityStatus>>>> GetPOByReceivingStatusAsync()
        {

            var poList = await _unitOfWork.PurchasingOrder.Query()
                .AsNoTracking()
                .Include(po => po.PurchasingOrderDetails)
                .Include(po => po.GoodReceiptNotes)
                    .ThenInclude(grn => grn.GoodReceiptNoteDetails)
                .ToListAsync();

            if (!poList.Any())
            {
                return new ServiceResult<Dictionary<string, IEnumerable<POQuantityStatus>>>
                {
                    Data = new(),
                    StatusCode = 200,
                    Message = "Không có đơn mua hàng nào."
                };
            }

            var result = ClassifyPOs(poList);

            return new ServiceResult<Dictionary<string, IEnumerable<POQuantityStatus>>>
            {
                Data = result,
                StatusCode = 200,
                Message = "Phân loại đơn mua hàng thành công"
            };
        }

        private Dictionary<string, IEnumerable<POQuantityStatus>> ClassifyPOs(List<PurchasingOrder> poList)
        {
            var fullyReceived = new List<POQuantityStatus>();
            var partiallyReceived = new List<POQuantityStatus>();
            var notReceived = new List<POQuantityStatus>();

            foreach (var po in poList)
            {
                var details = po.PurchasingOrderDetails ?? new List<PurchasingOrderDetail>();
                var grnDetails = po.GoodReceiptNotes?
                    .SelectMany(g => g.GoodReceiptNoteDetails ?? new List<GoodReceiptNoteDetail>())
                    .ToList() ?? new List<GoodReceiptNoteDetail>();


                var receivedMap = grnDetails
                    .GroupBy(g => (g.ProductID, g.UnitPrice))
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                int totalItems = details.Count;
                int fullyReceivedCount = 0;
                bool anyReceived = false;

                foreach (var pod in details)
                {
                    var key = (pod.ProductID, pod.UnitPrice);
                    if (receivedMap.TryGetValue(key, out var receivedQty))
                    {
                        anyReceived = true;
                        if (receivedQty >= pod.Quantity)
                            fullyReceivedCount++;
                    }
                }

                var dto = new POQuantityStatus { POID = po.POID, Status = po.Status };

                if (!anyReceived)
                    notReceived.Add(dto);
                else if (fullyReceivedCount == totalItems && totalItems > 0)
                    fullyReceived.Add(dto);
                else
                    partiallyReceived.Add(dto);
            }

            return new Dictionary<string, IEnumerable<POQuantityStatus>>
            {
                ["FullyReceived"] = fullyReceived,
                ["PartiallyReceived"] = partiallyReceived,
                ["NotReceived"] = notReceived
            };
        }


        public async Task<ServiceResult<bool>> DeletePOWithDraftStatus(int poid)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var po = await _unitOfWork.PurchasingOrder.Query()
                    .Include(p => p.PurchasingOrderDetails)
                    .FirstOrDefaultAsync(p => p.POID == poid && p.Status == PurchasingOrderStatus.draft);

                if (po == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Data = false,
                        Message = $"Không tìm thấy PO với id: {poid}",
                        Success = false,
                    };
                }

                _unitOfWork.PurchasingOrderDetail.RemoveRange(po.PurchasingOrderDetails);
                _unitOfWork.PurchasingOrder.Remove(po);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<bool>
                {
                    Data = true,
                    Message = $"Xóa thành công đơn hàng với id: {poid}",
                    StatusCode = 200,
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResult<bool>
                {
                    StatusCode = 400,
                    Message = $"Lỗi hệ thống khi xóa đơn hàng: {ex.Message}"
                };
            }
        }

        //Luật Doanh nghiệp 2020 và Nghị định 01/2021/NĐ-CP
        //Luật Dược số 105/2016/QH13, cùng Nghị định 54/2017/NĐ-CP (và Nghị định 155/2018/NĐ-CP sửa đổi), ngành bán buôn thuốc là ngành nghề kinh doanh có điều kiện. nợ trần : ko quá 3 lần equity
        public async Task<ServiceResult<POPaidViewDTO>> DepositedPOAsync(string userId, int poid, POUpdateDTO pOUpdateDTO)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Lấy đơn hàng theo POID
                var existingPO = await _unitOfWork.PurchasingOrder.Query()
                    .Include(po => po.Quotations)
                    .FirstOrDefaultAsync(po => po.POID == poid);

                if (existingPO == null)
                    return ServiceResult<POPaidViewDTO>.Fail($"Không tìm thấy đơn hàng với POID = {poid}", 404);

                // Kiểm tra tiền thanh toán không vượt tổng đơn hàng
                if (pOUpdateDTO.paid > existingPO.Total)
                    return ServiceResult<POPaidViewDTO>.Fail("Thanh toán vượt quá tổng giá trị đơn hàng", 400);

                // Cập nhật trạng thái đơn hàng (deposit lần 1)
                existingPO.Status = PurchasingOrderStatus.deposited;
                existingPO.Deposit = pOUpdateDTO.paid;
                existingPO.Debt = existingPO.Total - pOUpdateDTO.paid;
                existingPO.PaymentDate = DateTime.Now;
                existingPO.DepositDate = DateTime.Now;
                existingPO.PaymentBy = userId;

                _unitOfWork.PurchasingOrder.Update(existingPO);
                await _unitOfWork.CommitAsync();

                // Lấy thông tin người thanh toán
                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(existingPO.PaymentBy)
                           ?? throw new Exception("Không tìm thấy thông tin người thanh toán.");
                var paymentName = user.UserName;

                // Lấy báo giá (để biết nhà cung cấp tương ứng)
                var quotation = await _unitOfWork.Quotation.Query()
                    .FirstOrDefaultAsync(q => q.QID == existingPO.QID)
                    ?? throw new Exception($"Không tìm thấy báo giá cho đơn hàng POID = {existingPO.POID}");

                // Lấy thông tin tài chính của doanh nghiệp (để kiểm tra trần nợ)
                var pharmacySecretInfor = await _unitOfWork.PharmacySecretInfor.Query()
                    .FirstOrDefaultAsync(x => x.PMSID == 1)
                    ?? throw new Exception("Không tìm thấy thông tin tài chính của doanh nghiệp.");

                //Lấy hoặc tạo mới bản ghi công nợ cho nhà cung cấp
                var debtReport = await _unitOfWork.DebtReport.Query()
                    .FirstOrDefaultAsync(x => x.EntityType == DebtEntityType.Supplier && x.EntityID == quotation.SupplierID);

                if (debtReport == null)
                {
                    // Nếu chưa có, tạo mới công nợ
                    debtReport = new DebtReport
                    {
                        EntityID = quotation.SupplierID,
                        EntityType = DebtEntityType.Supplier,
                        Payables = existingPO.Debt,
                        Payday = existingPO.PaymentDate,
                        CreatedDate = DateTime.Now,
                        Status = DebtStatus.Apart
                    };
                    await _unitOfWork.DebtReport.AddAsync(debtReport);
                    await _unitOfWork.CommitAsync();
                }
                else
                {
                    // Nếu đã có, cập nhật nợ phải trả
                    debtReport.Payables += existingPO.Debt;
                    debtReport.Payday = existingPO.PaymentDate;
                }
                // cap nhat lai tong chi
                pharmacySecretInfor.TotalPaid += pOUpdateDTO.paid;
                _unitOfWork.PharmacySecretInfor.Update(pharmacySecretInfor);
                await _unitOfWork.CommitAsync();
                //
                var debtCeiling = pharmacySecretInfor.DebtCeiling;
                var currentDebt = debtCeiling - debtReport.Payables;

                if (debtReport.Payables > debtCeiling)
                {
                    debtReport.Status = DebtStatus.BadDebt;//2
                }
                else
                {
                    debtReport.Status = DebtStatus.Apart;//4
                }

                debtReport.CurrentDebt = currentDebt;
                _unitOfWork.DebtReport.Update(debtReport);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();


                var resultDto = new POPaidViewDTO
                {
                    PaymentBy = paymentName,
                    PaymentDate = existingPO.PaymentDate,
                    Status = existingPO.Status,
                    Debt = existingPO.Debt
                };

                return ServiceResult<POPaidViewDTO>.SuccessResult(resultDto, "Xác nhận tiền cọc thành công.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<POPaidViewDTO>.Fail($"Lỗi khi cập nhật đơn hàng: {ex.Message}", 500);
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
                    return ServiceResult<POPaidViewDTO>.Fail($"Không tìm thấy đơn hàng với POID = {poid}", 404);


                if (pOUpdateDTO.paid > existingPO.Debt)
                    return ServiceResult<POPaidViewDTO>.Fail("Thanh toán vượt quá số nợ còn lại.", 400);


                existingPO.Deposit += pOUpdateDTO.paid;
                existingPO.Debt = existingPO.Total - existingPO.Deposit;
                existingPO.PaymentDate = DateTime.Now;
                existingPO.PaymentBy = userId;
                existingPO.Status = existingPO.Debt == 0 ? PurchasingOrderStatus.compeleted : PurchasingOrderStatus.paid;

                _unitOfWork.PurchasingOrder.Update(existingPO);
                await _unitOfWork.CommitAsync();


                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(existingPO.PaymentBy)
                           ?? throw new Exception("Không tìm thấy thông tin người xác nhận thanh toán.");
                var paymentName = user.UserName;

                var quotation = await _unitOfWork.Quotation.Query()
                    .FirstOrDefaultAsync(q => q.QID == existingPO.QID)
                    ?? throw new Exception($"Không tìm thấy báo giá cho đơn hàng POID = {existingPO.POID}");

                // Lấy thông tin tài chính doanh nghiệp
                var pharmacySecretInfor = await _unitOfWork.PharmacySecretInfor.Query()
                    .FirstOrDefaultAsync(x => x.PMSID == 1)
                    ?? throw new Exception("Không tìm thấy thông tin tài chính doanh nghiệp.");

                var debtReport = await _unitOfWork.DebtReport.Query()
                    .FirstOrDefaultAsync(x => x.EntityType == DebtEntityType.Supplier && x.EntityID == quotation.SupplierID);

                if (debtReport == null)
                {
                    // Trường hợp không có nợ  là PO trước đã được thanh toán hết (Hơi vô lý nhưng thôi vẫn xử lý)
                    debtReport = new DebtReport
                    {
                        EntityID = quotation.SupplierID,
                        EntityType = DebtEntityType.Supplier,
                        Payables = 0,
                        CurrentDebt = pharmacySecretInfor.DebtCeiling,
                        Status = DebtStatus.Apart,
                        CreatedDate = DateTime.Now,
                        Payday = existingPO.PaymentDate
                    };
                    await _unitOfWork.DebtReport.AddAsync(debtReport);
                    await _unitOfWork.CommitAsync();
                }
                else
                {
                    // Cập nhật giảm nợ (thanh toán) 
                    debtReport.Payables -= pOUpdateDTO.paid;
                    if (debtReport.Payables < 0) debtReport.Payables = 0;
                    debtReport.Payday = existingPO.PaymentDate;
                    _unitOfWork.DebtReport.Update(debtReport);
                    await _unitOfWork.CommitAsync();
                }
                // tinh lai tong chi
                pharmacySecretInfor.TotalPaid += pOUpdateDTO.paid;
                _unitOfWork.PharmacySecretInfor.Update(pharmacySecretInfor);
                await _unitOfWork.CommitAsync();

                // Tính lại nợ trần và trạng thái
                var debtCeiling = pharmacySecretInfor.DebtCeiling;
                debtReport.CurrentDebt = debtCeiling - debtReport.Payables;
                _unitOfWork.DebtReport.Update(debtReport);
                await _unitOfWork.CommitAsync();
                // Mốc ngày bắt đầu tính hạn = DepositDate của đợt 1
                var depositDate = existingPO.DepositDate;
                var dueDate = depositDate.AddDays(existingPO.PaymentDueDate);
                if (debtReport.Payables > debtCeiling || DateTime.Now > dueDate)
                    debtReport.Status = DebtStatus.overTime;
                else if (debtReport.Payables > 0)
                    debtReport.Status = DebtStatus.Apart;//4
                else if (debtReport.Payables == 0)
                    debtReport.Status = DebtStatus.NoDebt;//5
                else if (debtReport.Payables > debtReport.CurrentDebt)
                    debtReport.Status = DebtStatus.BadDebt;//2
                _unitOfWork.DebtReport.Update(debtReport);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();


                var resultDto = new POPaidViewDTO
                {
                    PaymentBy = paymentName,
                    PaymentDate = existingPO.PaymentDate,
                    Status = existingPO.Status,
                    Debt = existingPO.Debt
                };

                return ServiceResult<POPaidViewDTO>.SuccessResult(resultDto, "Cập nhật thanh toán thành công.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<POPaidViewDTO>.Fail($"Lỗi khi cập nhật đơn hàng: {ex.Message}", 500);
            }
        }

        public async Task<ServiceResult<List<PharmacySecretInfor>>> PharmacySecretInfor()
        {
            try
            {
                var pharmacy = await _unitOfWork.PharmacySecretInfor.Query().ToListAsync();
                if (pharmacy == null)
                {
                    return new ServiceResult<List<PharmacySecretInfor>>
                    {
                        Success = false,
                        Data = null,
                        Message = "Lấy thông tin kinh doanh của công ty thất bại ",
                        StatusCode = 400,
                    };
                }
                return new ServiceResult<List<PharmacySecretInfor>>
                {
                    Success = true,
                    Data = pharmacy,
                    Message = "Lấy dữ liệu thành công",
                    StatusCode = 200,
                };
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PharmacySecretInfor>>.Fail($"Lỗi khi lấy thông tin kinh doanh thất bại: {ex.Message}", 500);
            }
        }

        public async Task<ServiceResult<List<DebtReportDTO>>> GetAllDebtReport()
        {
            try
            {
                var debtReports = await _unitOfWork.DebtReport.Query().ToListAsync();

                if (debtReports == null || !debtReports.Any())
                {
                    return new ServiceResult<List<DebtReportDTO>>
                    {
                        Success = false,
                        Data = null,
                        Message = "Không tìm thấy thông tin tài chính của công ty",
                        StatusCode = 400,
                    };
                }

                var supplierIds = debtReports.Select(d => d.EntityID).Where(id => id.HasValue).ToList();
                var suppliers = await _unitOfWork.Supplier.Query()
                    .Where(s => supplierIds.Contains(s.Id))
                    .ToListAsync();

                var result = debtReports.Select(d =>
                {
                    var supplier = suppliers.FirstOrDefault(s => s.Id == d.EntityID);
                    return new DebtReportDTO
                    {
                        EntityID = d.EntityID,
                        CreatedDate = d.CreatedDate,
                        CurrentDebt = d.CurrentDebt,
                        DebtName = supplier?.Name ?? "N/A",
                        EntityType = d.EntityType,
                        Payables = d.Payables,
                        Payday = d.Payday,
                        ReportID = d.ReportID,
                        Status = d.Status,
                    };
                }).ToList();

                return new ServiceResult<List<DebtReportDTO>>
                {
                    Success = true,
                    Data = result,
                    Message = "Lấy dữ liệu thành công",
                    StatusCode = 200,
                };
            }
            catch (Exception ex)
            {
                return ServiceResult<List<DebtReportDTO>>.Fail($"Lỗi khi lấy thông tin tài chính: {ex.Message}", 500);
            }
        }


        public async Task<ServiceResult<DebtReportDTO>> GetDebtReportDetail(int dbid)
        {
            try
            {

                var pharmacy = await _unitOfWork.DebtReport.Query()
                    .FirstOrDefaultAsync(db => db.ReportID == dbid);

                if (pharmacy == null)
                {
                    return new ServiceResult<DebtReportDTO>
                    {
                        Success = false,
                        Data = null,
                        Message = "Không tìm thấy thông tin tài chính",
                        StatusCode = 400,
                    };
                }


                var sup = await _unitOfWork.Supplier.Query()
                    .FirstOrDefaultAsync(s => s.Id == pharmacy.EntityID);

                if (sup == null)
                {
                    return new ServiceResult<DebtReportDTO>
                    {
                        Success = false,
                        Data = null,
                        Message = "Không tìm thấy thông tin tên người nợ",
                        StatusCode = 200,
                    };
                }

                var allPOs = await _unitOfWork.PurchasingOrder.Query()
                    .Include(po => po.Quotations)
                    .ToListAsync();

                var pos = await _unitOfWork.PurchasingOrder.Query().Include(po => po.Quotations).Where(po => po.Quotations.SupplierID == pharmacy.EntityID).ToListAsync();

                var viewDebtPOs = pos.Select(po => new ViewDebtPODTO
                {
                    poid = po.POID,
                    toatlPo = po.Total
                }).ToList();

                var result = new DebtReportDTO
                {
                    EntityID = pharmacy.EntityID,
                    CreatedDate = pharmacy.CreatedDate,
                    CurrentDebt = pharmacy.CurrentDebt,
                    DebtName = sup.Name,
                    EntityType = pharmacy.EntityType,
                    Payables = pharmacy.Payables,
                    Payday = pharmacy.Payday,
                    ReportID = pharmacy.ReportID,
                    Status = pharmacy.Status,
                    viewDebtPODTOs = viewDebtPOs
                };

                return new ServiceResult<DebtReportDTO>
                {
                    Success = true,
                    Data = result,
                    Message = "Lấy dữ liệu thành công",
                    StatusCode = 200,
                };
            }
            catch (Exception ex)
            {
                return ServiceResult<DebtReportDTO>.Fail($"Lỗi khi lấy thông tin tài chính: {ex.Message}", 500);
            }
        }

    }
}

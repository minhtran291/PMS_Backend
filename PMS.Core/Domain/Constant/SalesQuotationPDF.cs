using PMS.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace PMS.Core.Domain.Constant
{
    public static class QuotationTemplate
    {
        public static string GenerateQuotationHtml(SalesQuotation sq)
        {
            var rows = new StringBuilder();

            var sender = sq.StaffProfile.User.FullName ?? "";

            var receiverName = sq.RequestSalesQuotation.CustomerProfile.User.FullName ?? "";

            var receiverPhone = sq.RequestSalesQuotation.CustomerProfile.User.PhoneNumber ?? "";

            var receiverMST = sq.RequestSalesQuotation.CustomerProfile.Mst;

            var receiverAddress = sq.RequestSalesQuotation.CustomerProfile.User.Address;

            var validityTimeSpan = sq.QuotationDate.HasValue ? sq.ExpiredDate - sq.QuotationDate : null;

            string validityText;

            if (validityTimeSpan.HasValue)
            {
                var ts = validityTimeSpan.Value;

                if (ts.TotalDays >= 1)
                {
                    int days = (int)Math.Floor(ts.TotalDays);
                    validityText = $"{days} ngày";
                }
                else if (ts.TotalHours >= 1)
                {
                    int hours = (int)Math.Floor(ts.TotalHours);
                    validityText = $"{hours} giờ";
                }
                else
                {
                    int minutes = (int)Math.Ceiling(ts.TotalMinutes); // làm tròn lên
                    validityText = $"{minutes} phút";
                }
            }
            else
            {
                validityText = "Chưa xác định";
            }

            decimal subTotal = 0;   // Tổng chưa thuế
            decimal taxTotal = 0;   // Tổng thuế

            foreach (var item in sq.SalesQuotaionDetails)
            {
                var productName = HttpUtility.HtmlEncode(item.Product.ProductName);
                var unit = HttpUtility.HtmlEncode(item.Product.Unit);

                var note = item.Note ?? "";

                if(item.LotProduct != null)
                {
                    var taxText = HttpUtility.HtmlEncode(item.TaxPolicy?.Name);
                    decimal taxRate = item.TaxPolicy.Rate;
                    var expiredDate = item.LotProduct.ExpiredDate.ToString("dd/MM/yyyy");
                    var quantity = 1;
                    decimal salePrice = item.LotProduct.SalePrice;
                    decimal itemSubTotal = quantity * salePrice;
                    decimal itemTax = itemSubTotal * taxRate;
                    decimal itemTotal = itemSubTotal + itemTax;

                    subTotal += itemSubTotal;
                    taxTotal += itemTax;

                    rows.Append($@"
                    <tr>
                        <td>{productName}</td>
                        <td>{unit}</td>
                        <td>{taxText}</td>
                        <td>{expiredDate}</td>
                        <td>{quantity}</td>
                        <td>{salePrice:N0} ₫</td>
                        <td>{itemTotal:N0} ₫</td>
                        <td>{note}</td>
                    </tr>");
                }
                else
                {
                    rows.Append($@"
                    <tr>
                        <td>{productName}</td>
                        <td>{unit}</td>
                        <td>-</td>
                        <td>-</td>
                        <td>-</td>
                        <td>-</td>
                        <td>-</td>
                        <td>{note}</td>
                    </tr>");
                }
                
            }

            decimal grandTotal = subTotal + taxTotal;

            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Báo giá</title>
    <style>
        body {{ font-family: Arial, sans-serif; background: #fff; padding: 20px; }}
        h1 {{ color: #0078D7; text-align: center; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        th, td {{ border: 1px solid #ccc; padding: 8px; }}
        th {{ background: #f0f0f0; }}
        .total {{ text-align: right; font-weight: bold; }}
        .footer {{ text-align: center; margin-top: 40px; font-size: 12px; color: #777; }}
        .quotation-header {{
            display: flex;
            gap: 12px;           
            width: 100%;
            align-items: stretch;
            flex-wrap: nowrap;
        }}
        .sender, .receiver {{
            flex: 1 1 0;    
            min-width: 0;
            border: 1px solid #ccc;
            padding: 8px;
            box-sizing: border-box;
        }}
        .header-title {{font-weight: bold;
            text-transform: uppercase;
            margin-bottom: 8px;
            text-align: center;
            background-color: #f5f5f5;
            padding: 4px;
        }}
        .info-row {{
            display: flex;
            align-items: center;
            margin-bottom: 4px;
            gap: 4px;
        }}
        .info-row span.label {{
            min-width: 10px;
            font-weight: bold;
        }}
        .info-row span.value {{
            flex: 1 1 auto;
            min-width: 0;
            font-weight: normal;
        }}
    </style>
</head>
<body>
    <h1>BÁO GIÁ</h1>

    <div class=""quotation-header"">
        <!-- BÊN GỬI -->
        <div class=""sender"">
            <div class=""header-title"">BÊN GỬI</div>
            <div class=""info-row"">
                <span class=""label"">Tên nhà thuốc:</span>
                <span class=""value"">NHÀ THUỐC DƯỢC PHẨM SỐ 17</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Email:</span>
                <span class=""value"">minhtran2912003@gmail.com</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Địa chỉ:</span>
                <span class=""value"">Kiot số 17, Phường Lê Thanh Nghị, TP Hải Phòng</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Liên lạc:</span>
                <span class=""value"">0398233047</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Người gửi:</span>
                <span class=""value"">{HttpUtility.HtmlEncode(sender)}</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Mã báo giá:</span>
                <span class=""value"">{HttpUtility.HtmlEncode(sq.QuotationCode)}</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Ngày gửi:</span>
                <span class=""value"">{sq.QuotationDate:dd/MM/yyyy}</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Hiệu lực đến:</span>
                <span class=""value"">{sq.ExpiredDate:dd/MM/yyyy}</span>
            </div>
        </div>

        <!-- BÊN NHẬN -->
        <div class=""receiver"">
            <div class=""header-title"">BÊN NHẬN</div>
            <div class=""info-row"">
                <span class=""label"">Người nhận:</span>
                <span class=""value"">{HttpUtility.HtmlEncode(receiverName)}</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Số điện thoại:</span>
                <span class=""value"">{HttpUtility.HtmlEncode(receiverPhone)}</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Mã số thuế:</span>
                <span class=""value"">{HttpUtility.HtmlEncode(receiverMST)}</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Địa chỉ:</span>
                <span class=""value"">{HttpUtility.HtmlEncode(receiverAddress)}</span>
            </div>
        </div>
    </div>

    <h2 style=""text-align: center;"">Danh sách sản phẩm</h2>
    <table>
        <thead style=""text-align: center;"">
            <tr>
                <th>Tên sản phẩm</th>
                <th>Đơn vị</th>
                <th>Thuế</th>
                <th>Ngày hết hạn</th>
                <th width=""10%"">Số lượng tối thiểu</th>
                <th>Đơn giá</th>
                <th>Thành tiền</th>
                <th>Ghi chú</th>
            </tr>
        </thead>
        <tbody style=""text-align: center;"">
            {rows}
        </tbody>
        <tfoot>
            <tr>
                <td colspan=""8"" class=""total"">Tổng chưa thuế: {subTotal:N0} ₫</td>
            </tr>
            <tr>
                <td colspan=""8"" class=""total"">Thuế: {taxTotal:N0} ₫</td>
            </tr>
            <tr>
                <td colspan=""8"" class=""total"">Tổng cộng (đã bao gồm thuế): {grandTotal:N0} ₫</td>
            </tr>
        </tfoot>
    </table>

    <p><strong>Ghi chú</strong></p>

    <div style=""line-height:1.4;"">
        <div>Hiệu lực báo giá có giá trị {validityText} kể từ lúc báo giá.</div>
        <div>Quá thời hạn trên, giá chào trong bản báo giá này có thể được điều chỉnh theo thực tế.</div>
        <div>Tạm ứng {sq.DepositPercent.ToString("0.##")}% tiền cọc trong vòng {sq.DepositDueDays} ngày kể từ khi ký hợp đồng.</div>
        <div>Hàng hóa dự kiến giao trong thời gian {sq.ExpectedDeliveryDate} ngày kể từ ngày ký kết hợp đồng và cọc.</div>
        <div>Thanh toán bằng tiền mặt hoặc chuyển khoản vào tài khoản NGUYEN QUANG TRUNG - 4619300024210402 - Ngân hàng Timo.</div>
    </div>

    <div class=""footer"">
        <p>Cảm ơn quý khách đã quan tâm đến sản phẩm của chúng tôi.</p>
    </div>
</body>
</html>";
        }
    }

}

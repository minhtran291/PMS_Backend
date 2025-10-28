using PMS.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Core.Domain.Constant
{
    public static class QuotationTemplate
    {
        public static string GenerateQuotationHtml(SalesQuotation sq)
        {
            var rows = new StringBuilder();

            var validityTimeSpan = sq.QuotationDate.HasValue ? sq.ExpiredDate - sq.QuotationDate : null;

            if (sq.QuotationDate.HasValue)
            {
                validityTimeSpan = sq.ExpiredDate - sq.QuotationDate.Value;
            }

            string validityText;

            if (validityTimeSpan.HasValue)
            {
                var ts = validityTimeSpan.Value;

                if(ts.TotalDays >= 1)
                {
                    int days = (int)Math.Floor(ts.TotalDays);
                    validityText = $"{days} ngày";
                }
                else if(ts.TotalHours >= 1)
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
                var productName = HttpUtility.HtmlEncode(item.LotProduct?.Product?.ProductName ?? "N/A");
                var unit = HttpUtility.HtmlEncode(item.LotProduct?.Product?.Unit ?? "N/A");
                var taxText = HttpUtility.HtmlEncode(item.TaxPolicy.Name);
                decimal tax = item.TaxPolicy.Rate;
                var expiredDate = item.LotProduct?.ExpiredDate.ToString("dd/MM/yyyy") ?? "N/A";
                var quantity = 1;
                decimal salePrice = item.LotProduct?.SalePrice ?? 0;

                decimal itemSubTotal = quantity * salePrice;
                decimal itemTax = itemSubTotal * tax;
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
                    </tr>");
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
    </style>
</head>
<body>
    <h1>BÁO GIÁ</h1>
    <p><strong>Mã báo giá:</strong> {HttpUtility.HtmlEncode(sq.QuotationCode)}</p>
    <p><strong>Ngày gửi:</strong> {sq.QuotationDate:dd/MM/yyyy}</p>
    <p><strong>Hiệu lực đến:</strong> {sq.ExpiredDate:dd/MM/yyyy}</p>

    <table>
        <thead style=""text-align: center;"">
            <tr>
                <th>Tên sản phẩm</th>
                <th>Đơn vị</th>
                <th>Thuế</th>
                <th>Ngày hết hạn</th>
                <th>Số lượng tối thiểu</th>
                <th>Đơn giá</th>
                <th>Thành tiền</th>
            </tr>
        </thead>
        <tbody style=""text-align: center;"">
            {rows}
        </tbody>
        <tfoot>
            <tr>
                <td colspan=""6"" class=""total"">Tổng chưa thuế:</td>
                <td class=""total"">{subTotal:N0} ₫</td>
            </tr>
            <tr>
                <td colspan=""6"" class=""total"">Thuế:</td>
                <td class=""total"">{taxTotal:N0} ₫</td>
            </tr>
            <tr>
                <td colspan=""6"" class=""total"">Tổng cộng (đã bao gồm thuế):</td>
                <td class=""total"">{grandTotal:N0} ₫</td>
            </tr>
        </tfoot>
    </table>

    <p><strong>Ghi chú</strong></p>
    <p>Hiệu lực báo giá có giá trị {validityText} kể từ lúc báo giá</p>
    <p style=""white-space: pre-line;"">{HttpUtility.HtmlEncode(sq.SalesQuotationNote?.Content ?? "")}</p>

    <div class=""footer"">
        <p>Cảm ơn quý khách đã quan tâm đến sản phẩm của chúng tôi.</p>
    </div>
</body>
</html>";
        }
    }

}

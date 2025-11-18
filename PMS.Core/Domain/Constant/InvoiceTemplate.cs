using PMS.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Core.Domain.Constant
{
    public static class InvoiceTemplate
    {
        public static string GenerateInvoiceHtml(Invoice invoice)
        {
            var order = invoice.SalesOrder;

            var customerName = HttpUtility.HtmlEncode(order.Customer?.FullName ?? "");
            var customerPhone = HttpUtility.HtmlEncode(order.Customer?.PhoneNumber ?? "");
            var customerAddress = HttpUtility.HtmlEncode(order.Customer?.Address ?? "");

            var pharmacyName = "Nhà thuốc BBPharma";
            var pharmacyAddress = "Số 25, Tân Mỹ, Mỹ Đình, Hà Nội";
            var pharmacyTaxCode = "123456789";

            var sb = new StringBuilder();
            int index = 1;
            int exportIndex = 1;

            var details = invoice.InvoiceDetails
                .OrderBy(d => d.GoodsIssueNote.DeliveryDate)   // sửa đúng tên field ngày
                .ThenBy(d => d.GoodsIssueNoteId)
                .ToList();

            foreach (var d in details)
            {
                var note = d.GoodsIssueNote;

                                sb.Append($@"
                <tr>
                    <td style=""text-align:center"">{index}</td>
                    <td>PX{note.Id}</td>
                    <td style=""text-align:center"">{note.DeliveryDate:dd-MM-yyyy}</td>
                    <td style=""text-align:right"">{d.GoodsIssueAmount:N0}</td>
                    <td style=""text-align:right"">{d.AllocatedDeposit:N0}</td>
                    <td style=""text-align:right"">{d.PaidRemain:N0}</td>
                    <td style=""text-align:right"">{d.TotalPaidForNote:N0}</td>
                    <td style=""text-align:right"">{d.NoteBalance:N0}</td>
                    <td style=""text-align:center"">{exportIndex}</td>
                </tr>");

                index++;
                exportIndex++;
            }

            return $@"
            <!DOCTYPE html>
            <html lang=""vi"">
            <head>
                <meta charset=""UTF-8"">
                <title>Hóa đơn giá trị gia tăng</title>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        background:#fff;
                        margin:0;
                        padding:0;
                    }}

                    /* Trang A4 ~ 1120px cao (tương đối) */
                    .page {{
                        width: 100%;
                        height: 1320px;
                        padding: 40px 40px 60px 40px;
                        box-sizing: border-box;
                    }}

                    .wrapper {{
                        border:1px solid #000;
                        padding:20px 20px 30px 20px;
                        box-sizing:border-box;
                        height:100%;
                        display:flex;
                        flex-direction:column;
                        justify-content:space-between;
                    }}

                    h1 {{
                        text-align:center;
                        margin:0;
                        font-size:22px;
                    }}
                    h2 {{
                        text-align:center;
                        margin:4px 0 0 0;
                        font-size:13px;
                    }}

                    .top-row {{
                        display:flex;
                        justify-content:space-between;
                        margin-top:14px;
                        margin-bottom:14px;
                        font-size:11px;
                    }}

                    .box {{
                        border:1px solid #000;
                        padding:8px 10px;
                        font-size:11px;
                        margin-bottom:10px;
                    }}

                    .box div {{
                        margin-bottom:2px;
                    }}

                    table {{
                        width:100%;
                        border-collapse:collapse;
                        margin-top:8px;
                        font-size:11px;
                    }}

                    th, td {{
                        border:1px solid #000;
                        padding:5px 6px;
                    }}

                    th {{
                        background:#f5f5f5;
                        text-align:center;
                    }}

                    .right {{ text-align:right; }}

                    .content-area {{
                        flex: 1 1 auto;
                        display:flex;
                        flex-direction:column;
                    }}

                    .summary-table {{
                        width: 35%;
                        margin-top:10px;
                        margin-left:auto;
                        font-size:11px;
                    }}

                    .summary-table td {{
                        padding:3px 4px;
                        border:1px solid #000;
                    }}

                    .sign-area {{
                        display:flex;
                        justify-content:space-between;!Important
                        margin-top:40px;
                        font-size:11px;
                    }}

                    .sign-area div {{
                        text-align:center;
                    }}
                </style>
            </head>
            <body>
            <div class=""page"">
              <div class=""wrapper"">
                <!-- phần trên + nội dung chính -->
                <div class=""content-area"">
                    <div style=""text-align:center;"">
                        <h1>HÓA ĐƠN GIÁ TRỊ GIA TĂNG</h1>
                        <h2>(VAT INVOICE)</h2>
                    </div>

                    <div class=""top-row"">
                        <div>Mã CQT: Chưa có</div>
                        <div>Ngày: {invoice.IssuedAt:dd-MM-yyyy}</div>
                    </div>

                    <div class=""box"">
                        <div><strong>Nhà thuốc:</strong> {HttpUtility.HtmlEncode(pharmacyName)}</div>
                        <div><strong>Địa chỉ:</strong> {HttpUtility.HtmlEncode(pharmacyAddress)}</div>
                        <div><strong>Mã số thuế:</strong> {HttpUtility.HtmlEncode(pharmacyTaxCode)}</div>
                        <div><strong>Đồng tiền thanh toán:</strong> VND</div>
                        <br />
                        <div><strong>Mã hóa đơn:</strong> {HttpUtility.HtmlEncode(invoice.InvoiceCode)}</div>
                        <div><strong>Mã đơn hàng:</strong> {HttpUtility.HtmlEncode(order.SalesOrderCode)}</div>
                    </div>

                    <div class=""box"">
                        <div><strong>Khách hàng:</strong> {customerName}</div>
                        <div><strong>Số điện thoại:</strong> {customerPhone}</div>
                        <div><strong>Địa chỉ:</strong> {customerAddress}</div>
                        <div><strong>Hình thức thanh toán:</strong> TM/CK</div>
                    </div>

                    <table>
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>Mã phiếu xuất</th>
                                <th>Ngày xuất</th>
                                <th>Giá trị xuất</th>
                                <th>Phần cọc phân bổ</th>
                                <th>Tiền còn lại đã trả</th>
                                <th>Tổng đã thanh toán</th>
                                <th>Còn thiếu</th>
                                <th>Lần xuất hàng</th>
                            </tr>
                        </thead>
                        <tbody>
                            {sb}
                        </tbody>
                    </table>

                    <table class=""summary-table"">
                        <tr>
                            <td>Tổng cộng:</td>
                            <td class=""right"">{invoice.TotalAmount:N0}</td>
                        </tr>
                        <tr>
                            <td>Tổng cọc:</td>
                            <td class=""right"">{invoice.TotalDeposit:N0}</td>
                        </tr>
                        <tr>
                            <td>Tổng phần còn lại:</td>
                            <td class=""right"">{invoice.TotalRemain:N0}</td>
                        </tr>
                        <tr>
                            <td>Tổng đã thanh toán:</td>
                            <td class=""right"">{invoice.TotalPaid:N0}</td>
                        </tr>
                    </table>
                </div>

                <!-- phần chữ ký luôn nằm thấp hơn vì wrapper là flex-column space-between -->
                <div class=""sign-area"" >
                    <div>Người mua hàng<br /><br /><br /><br /></div>
                    <div>Người bán hàng<br /><br /><br /><br /></div>
                </div>
              </div>
            </div>
            </body>
            </html>";
        }
    }
}

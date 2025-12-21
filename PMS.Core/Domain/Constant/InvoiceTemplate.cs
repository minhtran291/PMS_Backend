using OfficeOpenXml.Style;
using PMS.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
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

            var pharmacyName = "Nhà thuốc dược phẩm số 17";
            var pharmacyAddress = "Kiot số 17, Phường Lê Thanh Nghị, Tp Hải Phòng";
            var pharmacyTaxCode = "030203002865";

            var sb = new StringBuilder();
            int index = 1;
            int exportIndex = 1;

            var details = invoice.InvoiceDetails
                .OrderBy(d => d.GoodsIssueNote.DeliveryDate)  
                .ThenBy(d => d.GoodsIssueNoteId)
                .ToList();

            var latestExportedAt = details
                .Select(x => x.GoodsIssueNote?.DeliveryDate)
                .Where(x => x.HasValue)
                .Max();

            var paymentDueAt = latestExportedAt?.AddDays(3);

            var paymentDueText = paymentDueAt.HasValue
                ? paymentDueAt.Value.ToString("dd-MM-yyyy")
                : "—";


            foreach (var d in details)
            {
                var note = d.GoodsIssueNote;
                sb.Append($@"
                <tr>
                    <td style=""text-align:center"">{index}</td>
                    <td>{note.GoodsIssueNoteCode}</td>
                    <td style=""text-align:center"">{note.DeliveryDate:dd-MM-yyyy}</td>
                    <td style=""text-align:center"">{d.GoodsIssueAmount:N0}</td>
                    <td style=""text-align:center"">{d.AllocatedDeposit:N0}</td>
                    <td style=""text-align:center"">{d.NoteBalance:N0}</td>
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
                <title>Phiếu Thu Tiền</title>
                <style>
                    html, body {{
                        margin: 0;
                        padding: 0;
                        height: 100%;
                    }}

                    body {{display: flex;
                        justify-content: center;
                        align-items: center;
                    }}                    

                    .page{{width: 250mm;
                        height: 350mm;
                        border: 1px solid #000;
                        box-sizing: border-box;
                        padding: 5mm;
                        font-family: Times New Roman, Times, serif;
                        font-size: 15px;

                        position: relative;
                        overflow: hidden;
                        background-color: #fff;
                    }}

                    @page{{
                        size: A4 portrait;
                        margin: 0;
                    }}

                    .bg-watermark{{position: absolute;
                        inset: 0;
                        position: absolute;
                        width: 100%;
                        height: 100%;
                        object-fit: cover;  
                        object-position: center;
                        opacity: 0.15;
                        z-index: 0;
                        transform: scale(1.25);
                    }}

                    .wrapper{{
                        height: 100%;
                        box-sizing: border-box;
                        display: flex;
                        flex-direction: column;
                        justify-content: space-between;
                        position: relative;
                        z-index: 1;
                    }}

                    h1 {{
                        text-align:center;
                        margin:0;
                        font-size:26px;
                    }}
                    h2 {{
                        text-align:center;
                        margin:4px 0 0 0;
                        font-size:17px;
                    }}

                    .top-row {{
                        display:flex;
                        justify-content:space-between;
                        margin-top:25px;
                        margin-bottom:15px;
                        font-size:15px;
                    }}

                    .box {{
                        border:1px solid #000;
                        padding:8px 10px;
                        font-size:15px;
                        margin-bottom:10px;
                    }}

                    .box div {{
                        margin-bottom:2px;
                    }}

                    table {{
                        width:100%;
                        border-collapse:collapse;
                        margin-top:8px;
                        font-size:15px;
                    }}

                    th, td {{
                        border:1px solid #000;
                        padding:6px 7px;
                    }}

                    th {{
                        background:#f5f5f5;
                        text-align:center;
                        font-size:15px;
                    }}

                    .right {{ text-align:right; }}

                    .content-area {{
                        flex: 1 1 auto;
                        display:flex;
                        flex-direction:column;
                    }}

                    .summary-table {{width: 35%;
                        margin-top:10px;
                        margin-left:auto;
                        font-size:15px;
                        border-collapse:collapse;
                        border:none; 
                    }}

                    .summary-table td {{padding:4px 6px;
                        border:none !important; 
                    }}

                    .sign-table {{width: 100%;
                        margin-top: 40px;
                        font-size: 15px;
                        border-collapse: collapse;
                    }}

                    .sign-table td {{border: none;
                        text-align: center;
                        height: 80px;  
                        vertical-align: top;
                    }}



                </style>
            </head>
            <body>
            <div class=""page"">
                <img class=""bg-watermark"" src=""https://api.bbpharmacy.site/assets/CTTNHHBBPHARMACY.png"" alt=""bg"" />
                  <div class=""wrapper"">
                    <!-- phần trên + nội dung chính -->
                    <div class=""content-area"">
                        <div style=""text-align:center;"">
                            <h1>Phiếu Thu Tiền</h1>
                        </div>

                        <div class=""top-row"">
                            <div>Ngày: {invoice.IssuedAt:dd-MM-yyyy}</div>
                            <div><strong>Hạn thanh toán hóa đơn này:</strong> {paymentDueText} (3 ngày sau ngày xuất kho)</div>
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
                                    <th>Ngày xuất kho</th>
                                    <th>Tổng tiền</th>
                                    <th>Đã thanh toán</th>
                                    <th>Chưa thanh toán</th>
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
                                <td>Tổng đã thanh toán:</td>
                                <td class=""right"">{invoice.TotalPaid:N0}</td>
                            </tr>
                            <tr>
                                <td>Tổng phần còn lại:</td>
                                <td class=""right"">{invoice.TotalRemain:N0}</td>
                            </tr>
                        
                        </table>
                    </div>

                    <table class=""sign-table"">
                        <tr>
                            <td>
                                <strong>Người mua hàng</strong><br />
                                (Ký, ghi rõ họ tên)
                            </td>
                            <td>
                                <strong>Người bán hàng</strong><br />
                                (Ký, ghi rõ họ tên)
                            </td>
                        </tr>
                    </table>

                 </div>
                </div>
                </body>
                </html>";
        }

        public static string GenerateLateReminderHtml(Invoice invoice)
        {
            var order = invoice.SalesOrder;

            var customerName = HttpUtility.HtmlEncode(order.Customer?.FullName ?? "");
            var customerPhone = HttpUtility.HtmlEncode(order.Customer?.PhoneNumber ?? "");
            var customerAddress = HttpUtility.HtmlEncode(order.Customer?.Address ?? "");

            var pharmacyName = "Nhà thuốc dược phẩm số 17";
            var pharmacyAddress = "Kiot số 17, Phường Lê Thanh Nghị, Tp Hải Phòng";
            var pharmacyTaxCode = "030203002865";

            var sb = new StringBuilder();
            int index = 1;
            int exportIndex = 1;

            var details = invoice.InvoiceDetails
                .OrderBy(d => d.GoodsIssueNote.DeliveryDate)
                .ThenBy(d => d.GoodsIssueNoteId)
                .ToList();

            var latestExportedAt = details
                .Select(x => x.GoodsIssueNote?.DeliveryDate)
                .Where(x => x.HasValue)
                .Max();

            var paymentDueAt = latestExportedAt?.AddDays(3);

            var paymentDueText = paymentDueAt.HasValue
                ? paymentDueAt.Value.ToString("dd-MM-yyyy")
                : "—";

            foreach (var d in details)
            {
                var note = d.GoodsIssueNote;
                sb.Append($@"
                <tr>
                    <td style=""text-align:center"">{index}</td>
                    <td>{note.GoodsIssueNoteCode}</td>
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
                <title>Nhắc Thanh Toán Hóa Đơn</title>
                <style>
                    html, body {{ margin:0; padding:0; height:100%; }}
                    body {{ display:flex; justify-content:center; align-items:center; }}
                    .page{{ width:250mm; height:350mm; border:1px solid #000; box-sizing:border-box; padding:5mm;
                           font-family: Times New Roman, Times, serif; font-size:15px; position:relative; overflow:hidden; background-color:#fff; }}
                    @page{{ size:A4 portrait; margin:0; }}
                    .bg-watermark{{ position:absolute; inset:0; width:100%; height:100%; object-fit:contain; opacity:0.3; z-index:0; transform:scale(0.85); }}
                    .wrapper{{ height:100%; box-sizing:border-box; display:flex; flex-direction:column; justify-content:space-between; position:relative; z-index:1; }}
                    h1 {{ text-align:center; margin:0; font-size:26px; }}
                    .top-row {{ display:flex; justify-content:space-between; margin-top:25px; margin-bottom:15px; font-size:15px; }}
                    .box {{ border:1px solid #000; padding:8px 10px; font-size:15px; margin-bottom:10px; }}
                    table {{ width:100%; border-collapse:collapse; margin-top:8px; font-size:15px; }}
                    th, td {{ border:1px solid #000; padding:6px 7px; }}
                    th {{ background:#f5f5f5; text-align:center; font-size:15px; }}
                    .right {{ text-align:right; }}
                    .summary-table {{ width:35%; margin-top:10px; margin-left:auto; font-size:15px; border-collapse:collapse; border:none; }}
                    .summary-table td {{ padding:4px 6px; border:none !important; }}
                </style>
            </head>
            <body>
            <div class=""page"">
                <img class=""bg-watermark"" src=""https://api.bbpharmacy.site/assets/CTTNHHBBPHARMACY.png"" alt=""bg"" />
                <div class=""wrapper"">
                    <div>
                        <div style=""text-align:center;"">
                            <h1>Nhắc Thanh Toán Hóa Đơn</h1>
                        </div>

                        <div class=""top-row"">
                            <div>Ngày: {DateTime.Now:dd-MM-yyyy}</div>
                            <div><strong>Hạn thanh toán:</strong> {paymentDueText}</div>
                        </div>

                        <div class=""box"">
                            <div><strong>Nhà thuốc:</strong> {HttpUtility.HtmlEncode(pharmacyName)}</div>
                            <div><strong>Địa chỉ:</strong> {HttpUtility.HtmlEncode(pharmacyAddress)}</div>
                            <div><strong>Mã số thuế:</strong> {HttpUtility.HtmlEncode(pharmacyTaxCode)}</div>
                            <br />
                            <div><strong>Mã hóa đơn:</strong> {HttpUtility.HtmlEncode(invoice.InvoiceCode)}</div>
                            <div><strong>Mã đơn hàng:</strong> {HttpUtility.HtmlEncode(order.SalesOrderCode)}</div>
                        </div>

                        <div class=""box"">
                            <div><strong>Khách hàng:</strong> {customerName}</div>
                            <div><strong>Số điện thoại:</strong> {customerPhone}</div>
                            <div><strong>Địa chỉ:</strong> {customerAddress}</div>
                        </div>

                        <table>
                            <thead>
                                <tr>
                                    <th>#</th>
                                    <th>Mã phiếu xuất</th>
                                    <th>Ngày xuất kho</th>
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
                            <tr><td>Tổng cộng:</td><td class=""right"">{invoice.TotalAmount:N0}</td></tr>
                            <tr><td>Tổng cọc:</td><td class=""right"">{invoice.TotalDeposit:N0}</td></tr>
                            <tr><td>Tổng đã thanh toán:</td><td class=""right"">{invoice.TotalPaid:N0}</td></tr>
                            <tr><td>Còn phải thanh toán:</td><td class=""right"">{invoice.TotalRemain:N0}</td></tr>
                        </table>
                    </div>
                </div>
            </div>
            </body>
            </html>";
        }
    }
}

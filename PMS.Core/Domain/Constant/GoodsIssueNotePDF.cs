using OfficeOpenXml.Style;
using PMS.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static System.Net.Mime.MediaTypeNames;

namespace PMS.Core.Domain.Constant
{
    public static class GoodsIssueNotePDF
    {
        public static string GenerateGoodsIssueNotePDF(GoodsIssueNote gin)
        {
            var logoServer = "https://api.bbpharmacy.site/assets/CTTNHHBBPHARMACY.png";
            var rows = new StringBuilder();

            int index = 1;

            foreach(var item in gin.GoodsIssueNoteDetails) //item.LotProduct != null ? item.LotProduct.Product.ProductName : "-"
            {
                var productName = HttpUtility.HtmlEncode(item.LotProduct?.Product.ProductName ?? "-");
                var quantity = item.Quantity;
                var location = HttpUtility.HtmlEncode(item.LotProduct?.WarehouseLocation.LocationName ?? "-");
                var expiredDate = item.LotProduct?.ExpiredDate.ToString("dd/MM/yyyy") ?? "-";

                rows.Append($@"
                <tr>
                    <td>{index++}</td>
                    <td>{productName}</td>
                    <td>{quantity}</td>
                    <td>{location}</td>
                    <td>{expiredDate}</td>
                </tr>");
            }

            var createdBy = gin.WarehouseStaff.FullName;
            var warehouseName = gin.Warehouse.Name;
            var salesOrderCode = gin.StockExportOrder.SalesOrder.SalesOrderCode;
            var seoCode = gin.StockExportOrder.StockExportOrderCode;

            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Phiếu Xuất Kho</title>
    <style>
        body {{font - family: Arial, sans-serif;
            background: #fff;
            padding: 20px;
        }}

        h1 {{color: #0078D7;
            text-align: center;
        }}

        table {{width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }}

        th,
        td {{border: 1px solid #ccc;
            padding: 8px;
        }}

        th {{background: #f0f0f0;
        }}

        .total {{text - align: right;
            font-weight: bold;
        }}

        .footer {{text - align: center;
            margin-top: 40px;
            font-size: 12px;
            color: #777;
        }}

        .quotation-header {{display: flex;
            gap: 12px;
            width: 100%;
            align-items: stretch;
            flex-wrap: nowrap;
        }}

        .header-title {{font - weight: bold;
            text-transform: uppercase;
            margin-bottom: 8px;
            text-align: center;
            background-color: #f5f5f5;
            padding: 4px;
        }}

        .info-row {{display: flex;
            align-items: center;
            margin-bottom: 4px;
            gap: 4px;
        }}

        .info-row span.label {{min - width: 120px;
            font-weight: bold;
        }}

        .info-row span.value {{flex: 1 1 auto;
            min-width: 0;
            font-weight: normal;
        }}

        .quotation-info {{margin - top: 20px;
        }}

        .quotation-info h3 {{text - align: center;
            margin-bottom: 20px;
        }}

        .quotation-info .info-row {{margin: 10px 0;
        }}

        .section-title {{background - color: #d9d9d9;
            text-align: center;
            font-weight: bold;
            padding: 6px;
            font-size: 13pt;
            margin-top: 15px;
        }}

        .signature-table td {{border: none;
            text-align: center;
            vertical-align: bottom;
            height: 80px;
            font-style: italic;
            padding-top: 30px;
        }}
    </style>
</head>

<body>
    <img src=""{logoServer}"" style=""width:120px; height:auto;"" />

    <h1>PHIẾU XUẤT KHO</h1>

    <div class=""quotation-header"">
        <h2>Thông tin phiếu xuất</h2>
    </div>

    <div class=""quotation-info"">
        <div class=""info-row"">
            <span class=""label"">Phiếu xuất kho:</span>
            <span class=""value"">{HttpUtility.HtmlEncode(gin.GoodsIssueNoteCode)}</span>
        </div>
        <div class=""info-row"">
            <span class=""label"">Mã yêu cầu:</span>
            <span class=""value"">{HttpUtility.HtmlEncode(seoCode)}</span>
        </div>
        <div class=""info-row"">
            <span class=""label"">Kho:</span>
            <span class=""value"">{HttpUtility.HtmlEncode(warehouseName)}</span>
        </div>
        <div class=""info-row"">
            <span class=""label"">Ngày tạo:</span>
            <span class=""value"">{gin.CreateAt:dd/MM/yyyy}</span>
        </div>
        <div class=""info-row"">
            <span class=""label"">Ngày xuất:</span>
            <span class=""value"">{gin.ExportedAt?.ToString("dd/MM/yyyy") ?? "-"}</span>
        </div>
        <div class=""info-row"">
            <span class=""label"">Người tạo:</span>
            <span class=""value"">{HttpUtility.HtmlEncode(createdBy)}</span>
        </div>
        <div class=""info-row"">
            <span class=""label"">Mã đơn hàng:</span>
            <span class=""value"">{HttpUtility.HtmlEncode(salesOrderCode)}</span>
        </div>
    </div>

    <h2 style=""text-align:center;"">Danh sách sản phẩm</h2>

    <table>
        <thead style=""text-align:center;"">
            <tr>
                <th>#</th>
                <th>Tên sản phẩm</th>
                <th>Số lượng</th>
                <th>Vị trí kho</th>
                <th>Hạn dùng</th>
            </tr>
        </thead>
        <tbody style=""text-align:center;"">
            {rows}
        </tbody>
    </table>
    
    <div class=""section-title"">CHỮ KÝ XÁC NHẬN</div>
    <table class=""signature-table"">
        <tr>
            <td><b>Đơn vị vận chuyển</b></td>
            <td><b>Thủ Kho</b></td>
        </tr>
        <tr>
            <td>(Ký, ghi rõ họ tên)</td>
            <td>(Ký, ghi rõ họ tên)</td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}

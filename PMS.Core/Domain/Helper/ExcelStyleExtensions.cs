using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml.Style;

namespace PMS.Core.Domain.Helper
{
    public static class ExcelStyleExtensions
    {
        public static void ApplyTitleStyle(this ExcelStyle style, int fontSize, Color bg)
        {
            style.Font.Bold = true;
            style.Font.Size = fontSize;
            style.Font.Color.SetColor(Color.White);
            style.Fill.PatternType = ExcelFillStyle.Solid;
            style.Fill.BackgroundColor.SetColor(bg);
            style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            style.VerticalAlignment = ExcelVerticalAlignment.Center;
            style.Border.BorderAround(ExcelBorderStyle.Medium);
        }

        public static void ApplyHeaderBox(this ExcelStyle style, Color bg)
        {
            style.Font.Bold = true;
            style.Fill.PatternType = ExcelFillStyle.Solid;
            style.Fill.BackgroundColor.SetColor(bg);
            style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            style.Border.BorderAround(ExcelBorderStyle.Medium);
        }

        public static void ApplyTableHeader(this ExcelStyle style, Color bg)
        {
            style.Font.Bold = true;
            style.Font.Color.SetColor(Color.White);
            style.Fill.PatternType = ExcelFillStyle.Solid;
            style.Fill.BackgroundColor.SetColor(bg);
            style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        public static void ApplyTitleSection(this ExcelStyle style, Color bg)
        {
            style.Font.Bold = true;
            style.Font.Size = 14;
            style.Fill.PatternType = ExcelFillStyle.Solid;
            style.Fill.BackgroundColor.SetColor(bg);
            style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        public static void ApplyThinBorder(this ExcelStyle style)
        {
            style.Border.Top.Style = ExcelBorderStyle.Thin;
            style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            style.Border.Left.Style = ExcelBorderStyle.Thin;
            style.Border.Right.Style = ExcelBorderStyle.Thin;
            style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }
    }

}

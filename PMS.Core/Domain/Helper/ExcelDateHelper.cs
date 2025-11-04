using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace PMS.Core.Domain.Helper
{
    public static class ExcelDateHelper
    {
        /// <summary>
        /// Đọc ngày từ một ô Excel (ExcelRange). thất bại sẽ ném Exception với message chứa fieldName.
        /// </summary>
        public static DateTime ReadDateFromCell(ExcelRange cell, string fieldName)
        {
            if (cell == null)
                throw new Exception($"{fieldName} - ô không tồn tại.");

            var val = cell.Value;
            var text = (cell.Text ?? string.Empty).Trim();

            if (val == null && string.IsNullOrWhiteSpace(text))
                throw new Exception($"{fieldName} bị trống.");

         
            if (val is double d)
                return DateTime.FromOADate(d);

            if (val is DateTime dt)
                return dt;

          
            var formats = new[]
            {
            "dd/MM/yyyy","d/M/yyyy","d-M-yyyy","dd-MM-yyyy",
            "MM/dd/yyyy","M/d/yyyy",
            "yyyy-MM-dd","yyyy/MM/dd",
            "dd/MM/yy","d/M/yy",
            "dd MMM yyyy","dd-MMM-yyyy","MMM dd yyyy","MMMM dd yyyy",
            "MMM yyyy","MMMM yyyy",
            "MM/yyyy","MM-yyyy",
            "dd-MMM","MMM dd"
        };

          
            text = text.Replace("Sept", "Sep", StringComparison.OrdinalIgnoreCase).Trim();

       
            if (DateTime.TryParseExact(text, formats, new CultureInfo("en-US"), DateTimeStyles.None, out var parsedExact))
                return parsedExact;

        
            if (DateTime.TryParseExact(text, formats, new CultureInfo("vi-VN"), DateTimeStyles.None, out parsedExact))
                return parsedExact;

            
            if (DateTime.TryParse(text, new CultureInfo("vi-VN"), DateTimeStyles.None, out var parsedVi))
                return parsedVi;

            if (DateTime.TryParse(text, new CultureInfo("en-US"), DateTimeStyles.None, out var parsedEn))
                return parsedEn;

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedLoose))
                return parsedLoose;

           
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double serial))
                return DateTime.FromOADate(serial);

            throw new Exception($"{fieldName} không thể parse được: '{text}'");
        }

        /// <summary>
        /// Parse ngày từ chuỗi chứa trong cột ExpiredDate tại một dòng cụ thể.
        /// Trả về DateTime hoặc ném exception nếu không parse được 
        /// </summary>
        public static DateTime ParseDateFromString(string rawText, int row)
        {
            var text = (rawText ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(text) || text.Equals("Chưa có lô hàng", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"ExpiredDate tại dòng {row} trống hoặc là 'Chưa có lô hàng'.");

           
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double serial))
                return DateTime.FromOADate(serial);

            var formats = new[]
            {
            "dd/MM/yyyy","d/M/yyyy","MM/yyyy","MM-yyyy",
            "MMM-yyyy","MMMM-yyyy","MMM-yy","MMMM-yy",
            "dd-MMM","dd-MMM-yyyy","MMM dd","MMM dd yyyy",
            "dd/MM/yy","M/d/yyyy","yyyy-MM-dd","yyyy/MM/dd",
            "dd MMM yyyy"
        };

            text = text.Replace("Sept", "Sep", StringComparison.OrdinalIgnoreCase).Trim();

            if (DateTime.TryParseExact(text, formats, new CultureInfo("en-US"), DateTimeStyles.None, out var parsedExact))
                return parsedExact;

            if (DateTime.TryParse(text, new CultureInfo("en-US"), DateTimeStyles.None, out var parsedLoose))
                return parsedLoose;

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedInvariant))
                return parsedInvariant;

            if (DateTime.TryParse(text, new CultureInfo("vi-VN"), DateTimeStyles.None, out var parsedVi))
                return parsedVi;

            
            throw new Exception($"Không parse được ExpiredDate '{text}' tại dòng {row}");
        }
    }
}

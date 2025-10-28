using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PRFQ
{
    public class PreviewProductDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public string DVT { get; set; } // Đơn vị tính
        public decimal UnitPrice { get; set; } // Giá nhập do supplier báo
        public string ExpiredDateDisplay { get; set; }

        public int CurrentQuantity { get; set; }
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }
        public int SuggestedQuantity { get; set; }
    }
}

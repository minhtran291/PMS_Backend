using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class ViewSalesQuotationDTO : SalesQuotationDTO
    {
        public List<ViewSalesQuotationDetailsDTO> Details { get; set; } = [];
        public List<SalesQuotationCommentDTO> Comments { get; set; } = [];
        public decimal? subTotal { get; set; }
        public decimal? taxTotal { get; set; }
        public decimal? grandTotal { get; set; }
        public string? note {  get; set; }
        public string PharmacyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenderAddress {  get; set; } = string.Empty;
        public string SenderPhone { get; set; } = string.Empty;
        public string SenderName {  get; set; } = string.Empty;
        public string ReceiverPhone {  get; set; } = string.Empty;
        public long? ReceiverMst {  get; set; }
        public string ReceiverAddress {  get; set; } = string.Empty;
    }
}

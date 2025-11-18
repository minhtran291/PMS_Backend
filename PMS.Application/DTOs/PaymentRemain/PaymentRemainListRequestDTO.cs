using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PaymentRemain
{
    public class PaymentRemainListRequestDTO
    {
        public int? SalesOrderId { get; set; }
        public int? GoodsIssueNoteId { get; set; }
        public PaymentStatus? Status { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public PaymentType? PaymentType { get; set; }
    }
}

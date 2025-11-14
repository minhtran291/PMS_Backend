using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class CustomerDebtRequestDTO
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int SalesOrderId { get; set; }
        public decimal DebtAmount { get; set; }
    }
}

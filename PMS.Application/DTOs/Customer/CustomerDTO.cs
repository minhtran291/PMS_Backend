using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.Customer
{
    public class CustomerDTO
    {
        public string Id { get; set; }
        public string UserName {  get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public  long? Mst { get; set; }

       
        public  string ImageCnkd { get; set; }

       
        public  string ImageByt { get; set; }

       
        public  long? Mshkd { get; set; }

        public UserStatus UserStatus { get; set; }

    }
}

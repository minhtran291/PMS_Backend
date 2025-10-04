using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.DTO.Request
{
    public class ResendConfirmEmailRequest
    {
        public required string EmailOrUsername {  get; set; }
    }
}

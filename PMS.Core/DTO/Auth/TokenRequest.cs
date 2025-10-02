using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.DTO.Auth
{
    public class TokenRequest
    {
        public string AccessToken { get; set; } = default!;
        // lam dau vao cho refresh token
    }
}

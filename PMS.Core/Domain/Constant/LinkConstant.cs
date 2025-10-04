using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Constant
{
    public class LinkConstant
    {
        public static readonly string baseUri = $"https://localhost:7213";
        
        public static UriBuilder UriBuilder(string controller, string path, string userId, string token)
        {
            var builder = new UriBuilder(baseUri)
            {
                Path = $"api/{controller}/{path}",

                Query = $"userId={userId}&token={Uri.EscapeDataString(token)}"
            };

            return builder;
        }
    }
}

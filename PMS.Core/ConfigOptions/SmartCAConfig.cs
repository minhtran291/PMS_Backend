using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.ConfigOptions
{
    public class SmartCAConfig
    {
        public string SpId { get; set; } = string.Empty;
        public string SpPassword { get; set; } = string.Empty;
        public string BaseUrlUat { get; set; } = string.Empty;
        public string BaseUrlProd { get; set; } = string.Empty;
        public bool UseProduction { get; set; }
        public string BaseUrl => UseProduction ? BaseUrlProd : BaseUrlUat;
    }
}

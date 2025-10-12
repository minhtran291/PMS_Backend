using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Constant
{
    public class ServiceResult<T>
    {
        public int StatusCode { get; set; }
        public bool Success => StatusCode == 200;
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}

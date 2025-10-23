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
        public bool Success {  get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ServiceResult<T> SuccessResult(T data, string message = "", int statusCode = 200)
    => new() { Success = true, Message = message, Data = data, StatusCode = statusCode };

        public static ServiceResult<T> Fail(string message, int statusCode = 400)
            => new() { Success = false, Message = message, StatusCode = statusCode };
    }
}

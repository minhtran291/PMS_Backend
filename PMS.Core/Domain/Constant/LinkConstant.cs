using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Constant
{
    public class LinkConstant
    {
        // FE URL để người dùng click xác nhận (dev)
        // Lưu ý: có thể đưa vào appsettings và inject qua IOptions trong tương lai
        //public static readonly string frontendBaseUri = $"http://localhost:3000";
        public static readonly string frontendBaseUri = $"https://bbpharmacy.site";
        public static readonly string backendBaseUri = $"https://localhost:7213";


        public static UriBuilder UriBuilder(string controller, string path, string userId, string token)
        {
            // Nếu là link xác nhận email -> điều hướng tới FE route /confirm-email
            if (controller == "User" && path == "confirm-email")
            {
                var fe = new UriBuilder(frontendBaseUri)
                {
                    Path = "confirm-email",
                    Query = $"userId={userId}&token={Uri.EscapeDataString(token)}"
                };
                return fe;
            }

            // Nếu là link đặt lại mật khẩu -> điều hướng tới FE route /reset-password
            if (controller == "User" && path == "reset-password")
            {
                var fe = new UriBuilder(frontendBaseUri)
                {
                    Path = "reset-password",
                    Query = $"userId={userId}&token={Uri.EscapeDataString(token)}"
                };
                return fe;
            }

            // Mặc định: trỏ tới BE API (giữ nguyên hành vi cũ cho các luồng khác như reset-password)
            var be = new UriBuilder(backendBaseUri)
            {
                Path = $"api/{controller}/{path}",
                Query = $"userId={userId}&token={Uri.EscapeDataString(token)}"
            };
            return be;
        }
    }
}

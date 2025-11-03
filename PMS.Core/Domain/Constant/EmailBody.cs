using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Constant
{
    public class EmailBody
    {
        public static string CONFIRM_EMAIL(string email, string link)
        {
            var body = $@"
    <!DOCTYPE html>
    <html lang=""en"">
    <head>
        <meta charset=""UTF-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <title>Xác nhận email</title>
    </head>
    <body style=""margin: 0; font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 40px;"">
        <div style=""max-width: 600px; margin: auto; background-color: #fff; border-radius: 10px; padding: 30px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);"">
            <img src=""https://res.cloudinary.com/dkyqm6vou/image/upload/v1737280894/OIP_3_ovpptg.jpg"" alt=""Envelope Icon"" style=""width: 60px; display: block; margin: 0 auto 20px;"">
            <h2 style=""text-align: center; color: #333;"">Xác nhận địa chỉ email</h2>
            <p style=""font-size: 15px; color: #555;"">Chào <strong>{email}</strong>,</p>
            <p style=""font-size: 15px; color: #555;"">
                Cảm ơn bạn đã đăng ký tài khoản tại <strong>PMS</strong>! Vui lòng nhấn nút bên dưới để xác nhận địa chỉ email của bạn:
            </p>
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{link}"" style=""background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold;"">Xác nhận ngay</a>
            </div>
            <p style=""font-size: 13px; color: #888;"">
                Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.
            </p>
            <hr style=""margin: 40px 0; border: none; border-top: 1px solid #eee;"">
            <p style=""font-size: 12px; color: #aaa; text-align: center;"">© {DateTime.Now.Year} PMS. All rights reserved.</p>
        </div>
    </body>
    </html>";
            return body;
        }

        public static string RESET_PASSWORD(string email, string link)
        {
            var body = $@"
<!DOCTYPE html>
    <html lang=""en"">
    <head>
        <meta charset=""UTF-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <title>Đặt lại mật khẩu</title>
    </head>
    <body style=""margin: 0; font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 40px;"">
        <div style=""max-width: 600px; margin: auto; background-color: #fff; border-radius: 10px; padding: 30px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);"">
            <img src=""https://res.cloudinary.com/dkyqm6vou/image/upload/v1737280894/OIP_3_ovpptg.jpg"" alt=""Envelope Icon"" style=""width: 60px; display: block; margin: 0 auto 20px;"">
            <h2 style=""text-align: center; color: #333;"">Quên mật khẩu</h2>
            <p style=""font-size: 15px; color: #555;"">Chào <strong>{email}</strong>,</p>
            <p style=""font-size: 15px; color: #555;"">
                Bạn hãy nhấn vào liên kết bên dưới để đặt lại mật khẩu:
            </p>
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{link}"" style=""background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold;"">Đặt lại mật khẩu</a>
            </div>
            <hr style=""margin: 40px 0; border: none; border-top: 1px solid #eee;"">
            <p style=""font-size: 12px; color: #aaa; text-align: center;"">© {DateTime.Now.Year} PMS. All rights reserved.</p>
        </div>
    </body>
    </html>";

            return body;
        }

        public static string SALES_QUOTATION(string email)
        {
            var body = $@"
<!DOCTYPE html>
    <html lang=""en"">
    <head>
        <meta charset=""UTF-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <title>Đơn báo giá</title>
    </head>
    <body style=""margin: 0; font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 40px;"">
        <div style=""max-width: 600px; margin: auto; background-color: #fff; border-radius: 10px; padding: 30px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);"">
            <img src=""https://res.cloudinary.com/dkyqm6vou/image/upload/v1737280894/OIP_3_ovpptg.jpg"" alt=""Envelope Icon"" style=""width: 60px; display: block; margin: 0 auto 20px;"">
            <h2 style=""text-align: center; color: #333;"">Đơn báo giá</h2>
            <p style=""font-size: 15px; color: #555;"">Chào <strong>{email}</strong>,</p>
            <p style=""font-size: 15px; color: #555;"">
                Chúng tôi gửi bạn đơn báo giá.
            </p>
            <p style=""font-size: 15px; color: #555;"">
                Nếu có bất kỳ thắc mắc nào vùi lòng liên hệ lại với chúng tôi.
            </p>
        </div>
    </body>
    </html>";

            return body;
        }
    }
}

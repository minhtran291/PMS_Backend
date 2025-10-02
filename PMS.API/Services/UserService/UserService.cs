using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using PMS.API.Services.BaseService;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using PMS.Core.DTO.Content;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.UserService
{
    public class UserService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration) : Service(unitOfWork, mapper), IUserService
    {
        private readonly string _jwtKey = configuration["JWT:SecretKey"];
        private readonly string _jwtIssuer = configuration["JWT:Issuer"];
        private readonly string _jwtAudience = configuration["JWT:Audience"];
        private readonly string _smtpHost = configuration["Email:Host"];
        private readonly int _smtpPort = configuration.GetValue<int>("Email:Port");
        private readonly string _smtpUsername = configuration["Email:Username"];
        private readonly string _smtpPassword = configuration["Email:Password"];
        private readonly string _fromEmail = configuration["Email:FromEmail"];
        private readonly string _fromName = configuration["Email:FromName"];
        private readonly string _baseUrl= configuration["ApplicationSettings:BaseUrl"];

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> InitiatePasswordResetAsync(string email)
        {
            var user = await unitOfWork.Users.FirstOrDefaultAsync(p => p.Email == email);
            if (user == null)
            {
                return false;
            }

            string token = GenerateJwtToken(email, null);

            await SendPasswordResetEmail(email, token);
            return true;
        }

        public async Task RegisterUserAsync(RegisterUser customer)
        {
            try
            {
                
                var validateEmailOrPhone = await unitOfWork.Users.FirstOrDefaultAsync(p => p.Email == customer.Email || p.PhoneNumber == customer.PhoneNumber);
                if (validateEmailOrPhone != null)
                {
                    throw new Exception("Email hoặc số điện thoại đã được sử dụng");
                }

               
                var user = new User
                {
                    UserName = customer.FullName,
                    Email = customer.Email,
                    PhoneNumber = customer.PhoneNumber,
                    CreateAt = customer.CreateAt,
                    UserStatus = customer.Status 
                };

               
                var createResult = await _unitOfWork.Users.UserManager.CreateAsync(user, customer.ConfirmPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new Exception($"Tạo người dùng thất bại: {errors}");
                }

                
                var profile = new PMS.Core.Domain.Entities.Profile
                {
                    UserId = user.Id,
                    Address = customer.Address,
                    FullName = customer.FullName,
                    Avatar = "https://as2.ftcdn.net/v2/jpg/03/31/69/91/1000_F_331699188_lRpvqxO5QRtwOM05gR50ImaaJgBx68vi.jpg",
                    Gender = Gender.Other,
                };
                await _unitOfWork.Profile.AddAsync(profile);

                
                var roleResult = await _unitOfWork.Users.UserManager.AddToRoleAsync(user, UserRoles.CUSTOMER);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Gán vai trò thất bại: {errors}");
                }
                await _unitOfWork.CommitAsync();
              
                string otp = GenerateOtp();
                string token = GenerateJwtToken(user.Email, otp);

                await SendVerificationEmail(user.Email, token);

              
            
            }
            catch (Exception ex)
            {
                throw new Exception($"Đăng ký thất bại: {ex.Message}");
            }
        }


        public async Task ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    throw new Exception("Mật khẩu mới không được để trống");
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtKey);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtIssuer,
                    ValidAudience = _jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                string email = jwtToken.Claims.First(x => x.Type == "email").Value;
                if (string.IsNullOrEmpty(email))
                {
                    throw new Exception("Không tìm thấy email trong token");
                }

                User user = (User)await _unitOfWork.Users.FirstOrDefaultAsync(p => p.Email == email);
                if (user == null)
                {
                    throw new Exception("Người dùng không tồn tại");
                }

                // Mã hóa mật khẩu mới 
                string hashedPassword = _unitOfWork.Users.UserManager.PasswordHasher.HashPassword(user, newPassword);
                user.PasswordHash = hashedPassword;
                var updateResult = await _unitOfWork.Users.UserManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    throw new Exception($"Đặt lại mật khẩu thất bại: {errors}");
                }
                await _unitOfWork.CommitAsync();
            }
            catch (SecurityTokenExpiredException)
            {
                throw new Exception("Token đã hết hạn");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                throw new Exception("Chữ ký token không hợp lệ");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi đặt lại mật khẩu: {ex.Message}");
            }
        }

        public Task<bool> UpdateUserAsync(User user)
        {
            throw new NotImplementedException();
        }

        public async Task VerifyJwtTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtKey);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtIssuer,
                    ValidAudience = _jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                string email = jwtToken.Claims.First(x => x.Type == "email").Value;
                if (string.IsNullOrEmpty(email))
                {
                    throw new Exception("Không tìm thấy email trong token");
                }


                User user = (User)await unitOfWork.Users.FirstOrDefaultAsync(p => p.Email == email);
                if (user == null)
                {
                    throw new Exception("Người dùng không tồn tại");
                }
                user.UserStatus = UserStatus.Active;
                user.EmailConfirmed = true;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.CommitAsync();
                if (user.UserStatus == UserStatus.Inactive)
                {
                    throw new Exception("Tài khoản chưa được kích hoạt");
                }
              
            }
            catch (SecurityTokenExpiredException)
            {
                throw new Exception("Token đã hết hạn");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                throw new Exception("Chữ ký token không hợp lệ");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi xác thực token: {ex.Message}");
            }
        }

        private async Task SendPasswordResetEmail(string email, string token)
        {

            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new System.Net.NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = true
            };


            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = "Đặt lại mật khẩu của bạn",
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            var resetLink = $"{_baseUrl}/api/User/reset-password?token={token}";
            mailMessage.Body = $"<strong>Nhấn vào link để đặt lại mật khẩu: <a href='{resetLink}'>Đặt lại mật khẩu</a></strong>";


            await client.SendMailAsync(mailMessage);
        }

        private async Task SendVerificationEmail(string email, string token)
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new System.Net.NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = "Xác thực tài khoản của bạn",
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            var verificationLink = $"{_baseUrl}/api/User/verify?token={token}";
            mailMessage.Body = $"<strong>Nhấn vào link để xác thực tài khoản: <a href='{verificationLink}'>Xác thực</a></strong>";

            await client.SendMailAsync(mailMessage);
        }

        private string GenerateOtp()
        {
            byte[] randomBytes = new byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
            int number = BitConverter.ToInt32(randomBytes, 0) % 900000 + 100000;
            return number.ToString();
        }

        private string GenerateJwtToken(string email, string? otp)
        {
            var claims = new List<Claim>
            {
                new Claim("email", email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrEmpty(otp))
            {
                claims.Add(new Claim("otp", otp));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(5),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}

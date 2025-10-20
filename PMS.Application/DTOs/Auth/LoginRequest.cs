using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email không được để chống")]
        public string UsernameOrEmail { get; set; } = null!;
        
        public string Password { get; set; } = null!;
    }
}

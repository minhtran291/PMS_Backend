namespace PMS.Application.DTOs.Auth
{
    public class TokenRequest
    {
        public string AccessToken { get; set; } = default!;
        // lam dau vao cho refresh token
    }
}

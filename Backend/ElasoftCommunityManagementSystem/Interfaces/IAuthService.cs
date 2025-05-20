using ElasoftCommunityManagementSystem.Services;

namespace ElasoftCommunityManagementSystem.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, string TempPassword)> Register(string name, string surname, string email, string phoneNumber, string? schoolNumber);
        Task<LoginResult?> Login(string email, string password);
        Task<LoginResult> RefreshToken(string refreshToken);
        Task RevokeRefreshToken(string refreshToken);
        
        Task<(string qrCodeUrl, string secretKey)> Enable2FA(int userId);
        Task<bool> Verify2FA(int userId, string code);
        Task Disable2FA(int userId);
        Task<bool> Validate2FA(int userId, string code);
        
        Task RequestPasswordReset(string email);
        Task<bool> ResetPassword(string email, string token, string newPassword);
    }
}

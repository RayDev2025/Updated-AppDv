namespace UserRole.Services
{
    public interface IEmailServices
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
        Task SendVerificationCodeAsync(string toEmail, string verificationCode);
        Task SendPasswordResetCodeAsync(string toEmail, string verificationCode); 
    }
}
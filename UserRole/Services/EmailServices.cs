using System.Net;
using System.Net.Mail;

namespace UserRole.Services
{
    public class EmailServices : IEmailServices
    {
        private readonly IConfiguration _config;

        public EmailServices(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:Port"]);
            var senderEmail = _config["EmailSettings:From"];
            var senderName = _config["EmailSettings:SenderName"];
            var username = _config["EmailSettings:Username"];
            var password = _config["EmailSettings:Password"];

            using var client = new SmtpClient(smtpServer, port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);
            await client.SendMailAsync(message);
        }

        public async Task SendVerificationCodeAsync(string toEmail, string verificationCode)
        {
            string subject = "Email Verification Code";
            string htmlMessage = $@"
                <html>
                <body>
                    <h2>Email Verification</h2>
                    <p>Your verification code is: <strong style='font-size: 24px; color: #007bff;'>{verificationCode}</strong></p>
                    <p>This code will expire in 10 minutes.</p>
                    <p>If you did not request this code, please ignore this email.</p>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, htmlMessage);
        }
        public async Task SendPasswordResetCodeAsync(string toEmail, string verificationCode)
        {
            string subject = "Password Reset Verification Code";
            string htmlMessage = $@"
        <html>
        <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
            <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; padding: 30px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
                <h2 style='color: #333; text-align: center; margin-bottom: 30px;'>🔑 Password Reset Request</h2>
                
                <p style='color: #666; font-size: 16px; line-height: 1.5;'>
                    We received a request to reset your password. To proceed with the password reset, please use the verification code below:
                </p>
                
                <div style='text-align: center; margin: 30px 0;'>
                    <div style='background-color: #007bff; color: white; font-size: 32px; font-weight: bold; padding: 20px; border-radius: 8px; letter-spacing: 8px; display: inline-block;'>
                        {verificationCode}
                    </div>
                </div>
                
                <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 5px; padding: 15px; margin: 20px 0;'>
                    <p style='margin: 0; color: #856404; font-size: 14px;'>
                        <strong>⚠️ Important:</strong>
                        <br>• This code will expire in 10 minutes
                        <br>• Do not share this code with anyone
                        <br>• If you didn't request this reset, please ignore this email
                    </p>
                </div>
                
                <p style='color: #666; font-size: 14px; text-align: center; margin-top: 30px;'>
                    This is an automated message, please do not reply to this email.
                </p>
            </div>
        </body>
        </html>";

            await SendEmailAsync(toEmail, subject, htmlMessage);
        }
    }
}
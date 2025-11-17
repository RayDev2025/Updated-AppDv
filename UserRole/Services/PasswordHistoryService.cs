using Microsoft.AspNetCore.Identity;
using UserRoles.Models;

namespace UserRoles.Services
{
    public class PasswordHistoryService
    {
        private readonly UserManager<Users> _userManager;
        private readonly IPasswordHasher<Users> _passwordHasher;

        public PasswordHistoryService(UserManager<Users> userManager, IPasswordHasher<Users> passwordHasher)
        {
            _userManager = userManager;
            _passwordHasher = passwordHasher;
        }

        public async Task<bool> IsPasswordReusedAsync(Users user, string newPassword)
        {
            // Get user's password history
            var passwordHistory = await GetPasswordHistoryAsync(user);

            // Check if new password matches any of the last 5 passwords
            foreach (var hashedPassword in passwordHistory)
            {
                var verificationResult = _passwordHasher.VerifyHashedPassword(user, hashedPassword, newPassword);
                if (verificationResult == PasswordVerificationResult.Success ||
                    verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return true; // Password is reused
                }
            }

            return false; // Password is not reused
        }

        public async Task SavePasswordToHistoryAsync(Users user, string newPassword)
        {
            var passwordHistory = await GetPasswordHistoryAsync(user);
            var hashedPassword = _passwordHasher.HashPassword(user, newPassword);

            // Add new password to history
            passwordHistory.Insert(0, hashedPassword);

            // Keep only last 5 passwords
            if (passwordHistory.Count > 5)
            {
                passwordHistory = passwordHistory.Take(5).ToList();
            }

            // Store in PasswordHistory field
            user.PasswordHistory = System.Text.Json.JsonSerializer.Serialize(passwordHistory);
            await _userManager.UpdateAsync(user);
        }

        private async Task<List<string>> GetPasswordHistoryAsync(Users user)
        {
            if (string.IsNullOrEmpty(user.PasswordHistory))
            {
                return new List<string>();
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.PasswordHistory) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
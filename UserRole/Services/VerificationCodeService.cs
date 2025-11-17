namespace UserRoles.Services
{
    public class VerificationCodeService
    {
        private static readonly Dictionary<string, VerificationData> _verificationCodes = new();

        public string GenerateCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public void StoreCode(string email, string code)
        {
            var normalizedEmail = email.ToLower();
            _verificationCodes[normalizedEmail] = new VerificationData
            {
                Code = code,
                ExpirationTime = DateTime.UtcNow.AddMinutes(10) 
            };
        }

        public bool ValidateCode(string email, string code)
        {
            var normalizedEmail = email.ToLower();
            if (_verificationCodes.TryGetValue(normalizedEmail, out var verificationData))
            {
                if (DateTime.UtcNow <= verificationData.ExpirationTime && verificationData.Code == code)
                {
                    _verificationCodes.Remove(normalizedEmail); 
                    return true;
                }
                else if (DateTime.UtcNow > verificationData.ExpirationTime)
                {
                    _verificationCodes.Remove(normalizedEmail); 
                }
            }
            return false;
        }

        public bool HasValidCode(string email)
        {
            var normalizedEmail = email.ToLower();
            if (_verificationCodes.TryGetValue(normalizedEmail, out var verificationData))
            {
                if (DateTime.UtcNow <= verificationData.ExpirationTime)
                {
                    return true;
                }
                else
                {
                    _verificationCodes.Remove(normalizedEmail);
                }
            }
            return false;
        }

        private class VerificationData
        {
            public string Code { get; set; }
            public DateTime ExpirationTime { get; set; }
        }
    }
}
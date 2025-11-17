using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace UserRoles.ViewModels
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Name is required.")]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "Password must be between {2} and {1} characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (!string.IsNullOrEmpty(Password))
            {
                if (!Regex.IsMatch(Password, @"[A-Z]"))
                {
                    results.Add(new ValidationResult(
                    "Password must contain at least one uppercase letter (A-Z).",
                    new[] { nameof(Password) }));
                }

                
                if (!Regex.IsMatch(Password, @"[a-z]"))
                {
                    results.Add(new ValidationResult(
                    "Password must contain at least one lowercase letter (a-z).",
                    new[] { nameof(Password) }));
                }

                
                if (!Regex.IsMatch(Password, @"[0-9]"))
                {
                    results.Add(new ValidationResult(
                    "Password must contain at least one number (0-9).",
                    new[] { nameof(Password) }));
                }

                
                if (!Regex.IsMatch(Password, @"[!@#$%^&*(),.?""':{}|<>]"))
                {
                    results.Add(new ValidationResult(
                    "Password must contain at least one special character (!@#$%^&*).",
                    new[] { nameof(Password) }));
                }

                
                var commonPasswords = new[] { "password", "123456", "123456789", "12345678",
"12345", "1234567", "1234567890", "qwerty", "abc123", "Password123" };

                if (commonPasswords.Any(cp => string.Equals(cp, Password, StringComparison.OrdinalIgnoreCase)))
                {
                    results.Add(new ValidationResult(
                    "Please choose a more secure password. Avoid common passwords.",
                    new[] { nameof(Password) }));
                }
            }

            return results;
        }
    }

    
    public class StrongPasswordAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null) return false;

            string password = value.ToString();

           
            bool hasUpper = Regex.IsMatch(password, @"[A-Z]");
            bool hasLower = Regex.IsMatch(password, @"[a-z]");
            bool hasDigit = Regex.IsMatch(password, @"[0-9]");
            bool hasSpecial = Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]");
            bool hasMinLength = password.Length >= 8;

            return hasUpper && hasLower && hasDigit && hasSpecial && hasMinLength;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} must contain at least 8 characters with uppercase, lowercase, number, and special character.";
        }
    }
}
using System.ComponentModel.DataAnnotations;

namespace UserRoles.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Verification code is required.")]
        [Display(Name = "Verification Code")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits.")]
        public string VerificationCode { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "Password must be between {2} and {1} characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [StrongPassword(ErrorMessage = "Password must contain at least 8 characters with uppercase, lowercase, number, and special character.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; }
    }
}
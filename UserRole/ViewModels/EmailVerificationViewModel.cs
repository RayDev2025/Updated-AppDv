using System.ComponentModel.DataAnnotations;

namespace UserRoles.ViewModels
{
    public class EmailVerificationViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Verification code is required.")]
        [Display(Name = "Verification Code")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits.")]
        public string VerificationCode { get; set; }
    }

    public class ResendVerificationCodeViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }
    }
}
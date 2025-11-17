using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // ADD THIS
using UserRole.Services;
using UserRoles.Models;
using UserRoles.Services;
using UserRoles.ViewModels;

namespace UserRoles.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly SignInManager<Users> _signInManager;
        private readonly IEmailServices _emailService;
        private readonly VerificationCodeService _verificationCodeService;
        private readonly ILogger<AccountController> _logger;
        private readonly PasswordHistoryService _passwordHistoryService;
        private const int MAX_LOGIN_ATTEMPTS = 3;
        private const int LOCKOUT_DURATION_MINUTES = 30;

        public AccountController(
            UserManager<Users> userManager,
            SignInManager<Users> signInManager,
            IEmailServices emailService,
            VerificationCodeService verificationCodeService,
            PasswordHistoryService passwordHistoryService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _verificationCodeService = verificationCodeService;
            _passwordHistoryService = passwordHistoryService;
            _logger = logger;
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    return View(model);
                }

                try
                {
                    var verificationCode = _verificationCodeService.GenerateCode();
                    _verificationCodeService.StoreCode(model.Email, verificationCode);

                    TempData["PendingRegistration"] = System.Text.Json.JsonSerializer.Serialize(model);

                    await _emailService.SendVerificationCodeAsync(model.Email, verificationCode);

                    TempData["Message"] = "A verification code has been sent to your email address.";
                    return RedirectToAction("VerifyEmail", new { email = model.Email });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending verification email to {Email}", model.Email);
                    ModelState.AddModelError("", "Failed to send verification email. Please try again.");
                }
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult VerifyEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register");
            }

            var model = new EmailVerificationViewModel { Email = email };
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(EmailVerificationViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (_verificationCodeService.ValidateCode(model.Email, model.VerificationCode))
                {
                    try
                    {
                        var pendingRegistrationJson = TempData["PendingRegistration"] as string;
                        if (string.IsNullOrEmpty(pendingRegistrationJson))
                        {
                            ModelState.AddModelError("", "Registration session expired. Please register again.");
                            return View(model);
                        }

                        var registerModel = System.Text.Json.JsonSerializer.Deserialize<RegisterViewModel>(pendingRegistrationJson);

                        // UPDATED: Set Role to "student" by default
                        var user = new Users
                        {
                            FullName = registerModel.Name,
                            UserName = registerModel.Email,
                            Email = registerModel.Email,
                            EmailConfirmed = true,
                            Role = "student" // ADD THIS LINE
                        };

                        var result = await _userManager.CreateAsync(user, registerModel.Password);

                        if (result.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, "User");

                            TempData["SuccessMessage"] = "Account created successfully! You can now login.";
                            return RedirectToAction("Login");
                        }
                        else
                        {
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError("", error.Description);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating user account for {Email}", model.Email);
                        ModelState.AddModelError("", "An error occurred while creating your account. Please try again.");
                    }
                }
                else
                {
                    ModelState.AddModelError("VerificationCode", "Invalid or expired verification code.");
                }
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationCode(ResendVerificationCodeViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var verificationCode = _verificationCodeService.GenerateCode();
                    _verificationCodeService.StoreCode(model.Email, verificationCode);

                    await _emailService.SendVerificationCodeAsync(model.Email, verificationCode);

                    TempData["Message"] = "A new verification code has been sent to your email address.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resending verification email to {Email}", model.Email);
                    TempData["ErrorMessage"] = "Failed to resend verification code. Please try again.";
                }
            }

            return RedirectToAction("VerifyEmail", new { email = model.Email });
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // UPDATED LOGIN METHOD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    // Check if account is locked
                    if (user.LockoutEndTime.HasValue && user.LockoutEndTime > DateTime.UtcNow)
                    {
                        var remainingLockoutTime = user.LockoutEndTime.Value - DateTime.UtcNow;
                        ModelState.AddModelError("", $"Account is locked due to too many failed attempts. Try again in {remainingLockoutTime.Minutes} minutes and {remainingLockoutTime.Seconds} seconds.");
                        return View(model);
                    }

                    // Check if email is confirmed
                    if (!user.EmailConfirmed)
                    {
                        ModelState.AddModelError("", "Please verify your email address before logging in.");
                        return View(model);
                    }

                    var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        // Reset failed login attempts
                        user.FailedLoginAttempts = 0;
                        user.LockoutEndTime = null;
                        await _userManager.UpdateAsync(user);

                        // SET SESSION VARIABLES FOR ENROLLMENT SYSTEM
                        HttpContext.Session.SetString("UserId", user.Id);
                        HttpContext.Session.SetString("Role", user.Role ?? "student");

                        // REDIRECT BASED ON ROLE
                        // In AccountController.cs Login method, after successful login:

                        if (user.Role == "admin")
                        {
                            return RedirectToAction("Dashboard", "Admin");
                        }
                        else if (user.Role == "professor")
                        {
                            return RedirectToAction("Dashboard", "Professor");
                        }
                        else
                        {
                            return RedirectToAction("EnrollmentForm", "Student");
                        }
                    }
                    else
                    {
                        user.FailedLoginAttempts++;

                        if (user.FailedLoginAttempts >= MAX_LOGIN_ATTEMPTS)
                        {
                            user.LockoutEndTime = DateTime.UtcNow.AddMinutes(LOCKOUT_DURATION_MINUTES);
                            ModelState.AddModelError("", $"Account locked due to {MAX_LOGIN_ATTEMPTS} failed login attempts. Try again in {LOCKOUT_DURATION_MINUTES} minutes.");
                        }
                        else
                        {
                            var remainingAttempts = MAX_LOGIN_ATTEMPTS - user.FailedLoginAttempts;
                            ModelState.AddModelError("", $"Invalid email or password. {remainingAttempts} attempt(s) remaining before account lockout.");
                        }

                        await _userManager.UpdateAsync(user);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                }
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear(); 
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !user.EmailConfirmed)
                {
                    TempData["Message"] = "If an account with this email exists, a password reset code has been sent.";
                    return View(model);
                }

                try
                {
                    var verificationCode = _verificationCodeService.GenerateCode();
                    _verificationCodeService.StoreCode(model.Email, verificationCode);

                    await _emailService.SendPasswordResetCodeAsync(model.Email, verificationCode);

                    TempData["Message"] = "A password reset code has been sent to your email address.";
                    return RedirectToAction("ResetPassword", new { email = model.Email });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending password reset email to {Email}", model.Email);
                    ModelState.AddModelError("", "Failed to send password reset email. Please try again.");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel { Email = email };
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (_verificationCodeService.ValidateCode(model.Email, model.VerificationCode))
                {
                    try
                    {
                        var user = await _userManager.FindByEmailAsync(model.Email);
                        if (user == null)
                        {
                            ModelState.AddModelError("", "User not found.");
                            return View(model);
                        }

                        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

                        var result = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

                        if (result.Succeeded)
                        {
                            TempData["SuccessMessage"] = "Your password has been reset successfully! You can now login with your new password.";
                            return RedirectToAction("Login");
                        }
                        else
                        {
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError("", error.Description);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error resetting password for {Email}", model.Email);
                        ModelState.AddModelError("", "An error occurred while resetting your password. Please try again.");
                    }
                }
                else
                {
                    ModelState.AddModelError("VerificationCode", "Invalid or expired verification code.");
                }
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendPasswordResetCode(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null && user.EmailConfirmed)
                {
                    var verificationCode = _verificationCodeService.GenerateCode();
                    _verificationCodeService.StoreCode(email, verificationCode);

                    await _emailService.SendPasswordResetCodeAsync(email, verificationCode);

                    TempData["Message"] = "A new password reset code has been sent to your email address.";
                }
                else
                {
                    TempData["Message"] = "If an account with this email exists, a password reset code has been sent.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending password reset email to {Email}", email);
                TempData["ErrorMessage"] = "Failed to resend password reset code. Please try again.";
            }

            return RedirectToAction("ResetPassword", new { email = email });
        }


        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            var model = new ChangePasswordFromDashboardViewModel
            {
                Email = User.Identity.Name
            };
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordFromDashboardViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                try
                {
                    // Check for password reuse
                    var isPasswordReused = await _passwordHistoryService.IsPasswordReusedAsync(user, model.NewPassword);
                    if (isPasswordReused)
                    {
                        ModelState.AddModelError("NewPassword", "You cannot reuse any of your last 5 passwords. Please choose a different password.");
                        return View(model);
                    }

                    // Verify current password is same as new password
                    var isSamePassword = await _userManager.CheckPasswordAsync(user, model.NewPassword);
                    if (isSamePassword)
                    {
                        ModelState.AddModelError("NewPassword", "New password must be different from your current password.");
                        return View(model);
                    }

                    // Generate reset token and change password
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

                    if (result.Succeeded)
                    {
                        // Save new password to history
                        await _passwordHistoryService.SavePasswordToHistoryAsync(user, model.NewPassword);

                        // Update security fields
                        user.LastPasswordChange = DateTime.UtcNow;
                        user.FailedLoginAttempts = 0;
                        user.LockoutEndTime = null;
                        await _userManager.UpdateAsync(user);

                        // Sign out user for security
                        await _signInManager.SignOutAsync();

                        TempData["SuccessMessage"] = "Your password has been changed successfully! Please login with your new password.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error changing password for user {UserId}", user.Id);
                    ModelState.AddModelError("", "An error occurred while changing your password. Please try again.");
                }
            }

            return View(model);
        }

    }
}
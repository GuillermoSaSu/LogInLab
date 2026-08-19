using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;
using LogInLab.Application.Services;
using LogInLab.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Claims;

namespace LogInLab.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly IMfaService _mfaService;

        public AccountController(IAuthService authService, IEmailVerificationService emailVerificationService, IPasswordResetService passwordResetService, IMfaService mfaService)
        {
            _authService = authService;
            _emailVerificationService = emailVerificationService;
            _passwordResetService = passwordResetService;
            _mfaService = mfaService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            RegisterRequest request = new RegisterRequest(model.Email, model.Password);
            AuthResult result = await _authService.RegisterAsync(request);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return View(model);
            }

            TempData["SuccessMessage"] = "Registration successful! Please check your email to verify your account.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string idAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userAgent = Request.Headers.UserAgent.ToString();

            LoginRequest requsest = new LoginRequest(model.Email, model.Password, idAddress, userAgent);
            LoginResult result = await _authService.LoginAsync(requsest);

            if (result.RequiresMfa)
            {
                TempData["PendingMfaUserId"] = result.UserId!.Value.ToString();
                return RedirectToAction("VerifyLoginMfa");
            }

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error");
                return View(model);
            }

            await SignInUserAsync(result.UserId!.Value, result.SessionId!.Value);

            return RedirectToAction("Index", "Profile");
        }

        private async Task SignInUserAsync(Guid userId, Guid sessionId)
        {
            List<Claim> claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new("SessionId", sessionId.ToString())
            };

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            string? sessionIdString = User.FindFirst("SessionId")?.Value;

            if (Guid.TryParse(sessionIdString, out Guid sessionId))
            {
                await _authService.LogoutAsync(sessionId);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var success = await _emailVerificationService.VerifyAsync(token);

            TempData["SuccessMessage"] = success ? "Email verified successfully!" : "Invalid or expired authentication link.";

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResendVerification()
        {
            return View(new ResendVerificationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerification(ResendVerificationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _emailVerificationService.ResendVerificationEmailAsync(model.Email);

            TempData["SuccessMessage"] = "If the account exists and it is not verified yet, a new authentication email has been sent";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _passwordResetService.RequestPasswordResetAsync(model.Email);

            TempData["SuccessMessage"] = "If the account exists, a password reset email has been sent";

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }
            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            AuthResult result = await _passwordResetService.ResetPasswordAsync(model.Token, model.NewPassword);
            
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error");
                return View(model);
            }

            TempData["SuccessMessage"] = "Password reset successful! You can now log in with your new password.";

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult VerifyLoginMfa()
        {
            if (TempData.Peek("PendingMfaUserId") is null)
            {
                return RedirectToAction("Login");
            }

            return View(new VerifyLoginMfaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyLoginMfa(VerifyLoginMfaViewModel model)
        {
            string? pendingUserIdRaw = TempData["PendingMfaUserId"]?.ToString(); 

            if (string.IsNullOrEmpty(pendingUserIdRaw) || !Guid.TryParse(pendingUserIdRaw, out Guid pendingUserId))
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                TempData.Keep("PendingMfaUserId");
                return View(model);
            }

            bool isValidCode = await _mfaService.ValidateCodeOrBackupAsync(pendingUserId, model.Code);

            if (!isValidCode) 
            {
                ModelState.AddModelError(string.Empty, "The code is not valid.");
                TempData.Keep("PendingMfaUserId");
                return View(model);
            }

            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userAgent = Request.Headers.UserAgent.ToString();

            LoginResult result = await _authService.CompleteMfaLoginAsync(pendingUserId, ipAddress, userAgent);

            if (!result.Success) 
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return View(model);
            }

            await SignInUserAsync(result.UserId!.Value, result.SessionId!.Value);

            return RedirectToAction("Index", "Profile");
        }
    }
}

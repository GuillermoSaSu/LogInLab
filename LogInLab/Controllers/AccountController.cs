using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;
using LogInLab.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal;
using System.Security.Claims;

namespace LogInLab.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
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

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error");
                return View(model);
            }

            List<Claim> claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.UserId!.Value.ToString()),
                new("SessionId", result.SessionId!.Value.ToString())
            };

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            return RedirectToAction("Index", "Profile");
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
    }
}

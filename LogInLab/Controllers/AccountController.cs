using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;
using LogInLab.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

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
            return Content("Login page not implemented yet.");
        }
    }
}

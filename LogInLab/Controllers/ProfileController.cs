using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LogInLab.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUserRepository _userRepository;

        public ProfileController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IActionResult> Index()
        {
            string? idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid userId = Guid.Parse(idClaim!);

            User? user = await _userRepository.GetByIdAsync(userId);

            ViewBag.UserId = userId;
            ViewBag.MfaEnabled = user?.MfaEnabled ?? false;

            return View();
        }
    }
}

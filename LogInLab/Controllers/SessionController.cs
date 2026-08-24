using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;
using LogInLab.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LogInLab.Controllers
{
    [Authorize]
    public class SessionController : Controller
    {
        private readonly ISessionManagementSerivce _sessionManagementSerivce;
        private readonly IAuthService _authService;
        
        public SessionController(ISessionManagementSerivce sessionManagementSerivce, IAuthService authService)
        {
            _sessionManagementSerivce = sessionManagementSerivce;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> Index() 
        {
            Guid userId = GetCurrentUserId();
            Guid currentSessionId = GetCurrentSessionId();

            List<SessionInfo> sessions = await _sessionManagementSerivce.GetActiveSessionAsync(userId);

            List<SessionViewModel> viewModel = sessions.Select(s => new SessionViewModel
            {
                Id = s.Id,
                IpAddress = s.IpAddress,
                UserAgent = s.UserAgent,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                IsCurentSession = s.Id == currentSessionId
            }).OrderByDescending(s => s.IsCurentSession)
            .ThenByDescending(s => s.CreatedAt)
            .ToList();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(Guid sessionId)
        {
            Guid userId = GetCurrentUserId();
            Guid currentSessionId = GetCurrentSessionId();
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userAgent = Request.Headers.UserAgent.ToString();

            if (sessionId == currentSessionId)
            {
                await _authService.LogoutAsync(currentSessionId, ipAddress, userAgent, userId);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account");
            }

            await _sessionManagementSerivce.RevokeSessionAsync(userId, sessionId, currentSessionId);

            TempData["SuccessMessage"] = "The session has been closed successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAllOthers(){
            Guid userId = GetCurrentUserId();
            Guid currentSessionId = GetCurrentSessionId();
            await _sessionManagementSerivce.RevokeAllOtherSessionsAsync(userId, currentSessionId);

            TempData["SuccessMessage"] = "All sessions has been closed successfully";
            return RedirectToAction("Index");
        }

        private Guid GetCurrentUserId()
        {
            string? idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(idClaim))
            { 
                return Guid.Parse(idClaim);
            }
            return Guid.Empty;
        }

        private Guid GetCurrentSessionId()
        {
            string? sessionIdClaim = User.FindFirstValue("SessionId");
            if (!string.IsNullOrEmpty(sessionIdClaim))
            {
                return Guid.Parse(sessionIdClaim);
            }
            return Guid.Empty;
        }
    }
}

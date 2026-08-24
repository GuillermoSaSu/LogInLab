using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;
using LogInLab.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Security.Claims;

namespace LogInLab.Controllers
{
    [Authorize]
    public class MfaController : Controller
    {
        private readonly IMfaService _mfaService;

        public MfaController(IMfaService mfaService)
        {
            _mfaService = mfaService;
        }

        [HttpGet]
        public async Task<IActionResult> Setup()
        {
            Guid userId = GetCurrentUserId();
            string userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "user";

            MfaSetupResult setupResult = await _mfaService.BeingSetupAsync(userId, userEmail);

            string qrCodeBase64 = GenerateQrCodeBase64(setupResult.QrCodeUri);

            MfaSetupViewModel viewModel = new MfaSetupViewModel
            {
                SecretKey = setupResult.SecretKey,
                QrCodeBase64Image = qrCodeBase64
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmSetup(MfaConfirmViewModel model)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userAgent = Request.Headers.UserAgent.ToString();

            if (!ModelState.IsValid)
            {
                return View("Setup", model);
            }

            Guid userId = GetCurrentUserId();
            MfaActivationResult result = await _mfaService.ConfirmSetupAsync(ipAddress, userAgent, userId, model.Code);

            if (!result.Success) 
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);

                string userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "user";
                MfaSetupResult setupResult = await _mfaService.BeingSetupAsync(userId, userEmail);
                string qrCodeBase64 = GenerateQrCodeBase64(setupResult.QrCodeUri);

                return View("Setup", new MfaSetupViewModel
                {
                    SecretKey= setupResult.SecretKey,
                    QrCodeBase64Image= qrCodeBase64
                });
            }

            TempData["BackupCodes"] = string.Join(',', result.BackupCodes);
            return RedirectToAction("BackupCodes");
        }

        [HttpGet]
        public IActionResult BackupCodes()
        {
            string codesRaw = TempData["BackupCodes"] as string;
            if (string.IsNullOrEmpty(codesRaw))
            {
                return RedirectToAction("Index", "Profile");
            }

            MfaBackupCodesViewmodel viewModel = new MfaBackupCodesViewmodel
            {
                BackupCodes = codesRaw.Split(',').ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable()
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userAgent = Request.Headers.UserAgent.ToString();

            Guid userId = GetCurrentUserId();
            await _mfaService.DisableAsync(ipAddress, userAgent, userId);

            TempData["SuccessMessage"] = "Two-steps authentication has been disabled.";
            return RedirectToAction("Index", "Profile");
        }

        private Guid GetCurrentUserId()
        {
            string? idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(idClaim);
        }

        private static string GenerateQrCodeBase64(string qrCodeUri)
        {
            using QRCodeGenerator qrGenerator = new QRCodeGenerator();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
            using PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] bytes = qrCode.GetGraphic(20);

            return Convert.ToBase64String(bytes);
        }
    }
}

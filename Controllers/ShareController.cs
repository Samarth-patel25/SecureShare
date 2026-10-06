using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureShare.Data;
using SecureShare.Models;
using SecureShare.Services;
using FileShare = SecureShare.Models.FileShare;
using Microsoft.AspNetCore.Identity;

namespace SecureShare.Controllers
{
    [Authorize]
    public class ShareController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _encryptionService;
        private readonly IPasswordHasher<FileShare> _passwordHasher;

        public ShareController(ApplicationDbContext context,EncryptionService encryption, IPasswordHasher<FileShare> passwordHasher)
        {
            _context = context;
            _encryptionService = encryption;
            _passwordHasher = passwordHasher;
        }

        [HttpPost]
        public async Task<IActionResult> Create(int fileId, int expiryHours, int maxDownloads, string? sharePassword)
        {
            string userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )!.Value;

            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

            if (file == null)
            {
                return NotFound();
            }

            byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);

            string shareToken = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            DateTime? expiresAt = null;

            if (expiryHours > 0)
            {
                expiresAt = DateTime.UtcNow.AddHours(expiryHours);
            }



            var fileShare = new FileShare
            {
                FileId = file.Id,
                OwnerId = userId,
                ShareToken = shareToken,
                ExpiresAt = expiresAt,
                MaxDownloads = maxDownloads > 0 ? maxDownloads : null
            };

            if (!string.IsNullOrWhiteSpace(sharePassword))
            {
                fileShare.IsPasswordProtected = true;
                fileShare.PasswordHash = _passwordHasher.HashPassword(
                    fileShare,
                    sharePassword
                );
            }

            _context.FileShares.Add(fileShare);

            await _context.SaveChangesAsync();

            string shareLink = Url.Action(
                "Access",
                "Share",
                new { token = shareToken },
                Request.Scheme
            )!;

            return Content(shareLink);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Access(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return NotFound();
            }

            var fileShare = await _context.FileShares
                .FirstOrDefaultAsync(s => s.ShareToken == token);

            if (fileShare == null)
            {
                return NotFound();
            }

            if (fileShare.IsRevoked)
            {
                return Content("This share link has been revoked.");
            }

            if (fileShare.ExpiresAt.HasValue &&
                fileShare.ExpiresAt.Value < DateTime.UtcNow)
            {
                return Content("This share link has expired.");
            }

            if (fileShare.MaxDownloads.HasValue &&
                fileShare.DownloadCount >= fileShare.MaxDownloads.Value)
            {
                return Content("This share link has reached its download limit.");
            }

            if (fileShare.IsPasswordProtected)
            {
                var model = new SharePasswordViewModel
                {
                    Token = token
                };

                return View("Password", model);
            }

            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileShare.FileId);

            if (file == null)
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(file.FilePath))
            {
                return NotFound();
            }

            byte[] decryptedFile = await _encryptionService.DecryptFileAsync(
                file.FilePath
            );

            fileShare.DownloadCount++;

            await _context.SaveChangesAsync();

            return File(
                decryptedFile,
                file.ContentType,
                file.OriginalFileName
            );
        }

        [HttpPost]
        public async Task<IActionResult> Revoke(int id)
        {
            string userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )!.Value;

            var fileShare = await _context.FileShares
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId);

            if (fileShare == null)
            {
                return NotFound();
            }

            fileShare.IsRevoked = true;

            await _context.SaveChangesAsync();

            return RedirectToAction("MyFiles", "File");
        }

        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            string userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )!.Value;

            var shares = await _context.FileShares
                .Include(s => s.File)
                .Where(s => s.OwnerId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(shares);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> VerifyPassword(SharePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Password", model);
            }

            var fileShare = await _context.FileShares
                .FirstOrDefaultAsync(s => s.ShareToken == model.Token);

            if (fileShare == null)
            {
                return NotFound();
            }

            if (fileShare.IsRevoked)
            {
                return Content("This share link has been revoked.");
            }

            if (fileShare.ExpiresAt.HasValue &&
                fileShare.ExpiresAt.Value < DateTime.UtcNow)
            {
                return Content("This share link has expired.");
            }

            if (fileShare.MaxDownloads.HasValue &&
                fileShare.DownloadCount >= fileShare.MaxDownloads.Value)
            {
                return Content("This share link has reached its download limit.");
            }

            if (!fileShare.IsPasswordProtected ||
                string.IsNullOrEmpty(fileShare.PasswordHash))
            {
                return RedirectToAction(
                    "Access",
                    new { token = model.Token }
                );
            }

            var result = _passwordHasher.VerifyHashedPassword(
                fileShare,
                fileShare.PasswordHash,
                model.Password
            );

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "Password",
                    "Incorrect password."
                );

                return View("Password", model);
            }

            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileShare.FileId);

            if (file == null)
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(file.FilePath))
            {
                return NotFound();
            }

            byte[] decryptedFile = await _encryptionService.DecryptFileAsync(
                file.FilePath
            );

            fileShare.DownloadCount++;

            await _context.SaveChangesAsync();

            return File(
                decryptedFile,
                file.ContentType,
                file.OriginalFileName
            );
        }
    }
}
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureShare.Data;
using SecureShare.Models;
using SecureShare.Services;
using FileShare = SecureShare.Models.FileShare;

namespace SecureShare.Controllers
{
    [Authorize]
    public class ShareController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _encryptionService;

        public ShareController(ApplicationDbContext context,EncryptionService encryption)
        {
            _context = context;
            _encryptionService = encryption;
        }

        [HttpPost]
        public async Task<IActionResult> Create(int fileId, int expiryHours, int maxDownloads)
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
    }
}
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
        private readonly UserManager<IdentityUser> _userManager;

        public ShareController(ApplicationDbContext context,EncryptionService encryption, IPasswordHasher<FileShare> passwordHasher,
                UserManager<IdentityUser> userManager)
        {
            _context = context;
            _encryptionService = encryption;
            _passwordHasher = passwordHasher;
            _userManager = userManager;
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

            TempData["ShareLink"] = shareLink;
            TempData["SharedFileId"] = file.Id;

            return RedirectToAction("MyFiles", "File");
        }

        [HttpPost]
        public async Task<IActionResult> ShareWithUser(int fileId,string email)
        {
            string ownerId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )!.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ShareUserError"] = "Please enter an email address.";

                return RedirectToAction("MyFiles", "File");
            }

            var file = await _context.Files
                .FirstOrDefaultAsync(f =>
                    f.Id == fileId &&
                    f.UserId == ownerId);

            if (file == null)
            {
                return NotFound();
            }

            var recipient = await _userManager.FindByEmailAsync(
                email.Trim()
            );

            if (recipient == null)
            {
                TempData["ShareUserError"] =
                    "No SecureShare account was found with that email address.";

                TempData["ShareUserFileId"] = fileId;

                return RedirectToAction("MyFiles", "File");
            }

            if (recipient.Id == ownerId)
            {
                TempData["ShareUserError"] =
                    "You cannot share a file with yourself.";

                TempData["ShareUserFileId"] = fileId;

                return RedirectToAction("MyFiles", "File");
            }

            var existingPermission = await _context.FilePermissions
                .FirstOrDefaultAsync(p =>
                    p.FileId == fileId &&
                    p.UserId == recipient.Id);

            if (existingPermission != null)
            {
                if (!existingPermission.IsRevoked)
                {
                    TempData["ShareUserError"] =
                        "This file is already shared with this user.";
                }
                else
                {
                    existingPermission.IsRevoked = false;
                    existingPermission.Permission = "Viewer";
                    existingPermission.CreatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    TempData["ShareUserSuccess"] =
                        $"File shared with {recipient.Email}.";
                }

                TempData["ShareUserFileId"] = fileId;

                return RedirectToAction("MyFiles", "File");
            }

            var permission = new FilePermission
            {
                FileId = fileId,
                UserId = recipient.Id,
                Permission = "Viewer",
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            _context.FilePermissions.Add(permission);

            await _context.SaveChangesAsync();

            TempData["ShareUserSuccess"] =
                $"File shared with {recipient.Email}.";

            TempData["ShareUserFileId"] = fileId;

            return RedirectToAction("MyFiles", "File");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Access(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return View("Error", new SharePasswordViewModel
                {
                    ErrorMessage = "This share link is invalid."
                });
            }

            var fileShare = await _context.FileShares
                .Include(s => s.File)
                .FirstOrDefaultAsync(s => s.ShareToken == token);

            if (fileShare == null)
            {
                return View("Error", new SharePasswordViewModel
                {
                    ErrorMessage = "This share link is invalid or no longer exists."
                });
            }

            var model = new SharePasswordViewModel
            {
                Token = token,
                FileName = fileShare.File?.OriginalFileName,
                ContentType = fileShare.File?.ContentType,
                RequiresPassword = fileShare.IsPasswordProtected
            };

            if (fileShare.IsRevoked)
            {
                model.ErrorMessage = "This share link has been revoked.";

                if (fileShare.IsPasswordProtected)
                {
                    return View("Password", model);
                }

                return View("Error", model);
            }

            if (fileShare.ExpiresAt.HasValue &&
                fileShare.ExpiresAt.Value < DateTime.UtcNow)
            {
                model.ErrorMessage = "This share link has expired.";

                if (fileShare.IsPasswordProtected)
                {
                    return View("Password", model);
                }

                return View("Error", model);
            }

            if (fileShare.MaxDownloads.HasValue &&
                fileShare.DownloadCount >= fileShare.MaxDownloads.Value)
            {
                model.ErrorMessage = "This share link has reached its download limit.";

                if (fileShare.IsPasswordProtected)
                {
                    return View("Password", model);
                }

                return View("Error", model);
            }

            if (fileShare.File == null)
            {
                model.ErrorMessage = "The shared file could not be found.";

                return View("Error", model);
            }

            if (!System.IO.File.Exists(fileShare.File.FilePath))
            {
                model.ErrorMessage = "The shared file is no longer available.";

                return View("Error", model);
            }

            // Password-protected file
            if (fileShare.IsPasswordProtected)
            {
                return View("Password", model);
            }

            // Unprotected file - download directly
            byte[] decryptedFile = await _encryptionService.DecryptFileAsync(
                fileShare.File.FilePath
            );

            fileShare.DownloadCount++;

            await _context.SaveChangesAsync();

            return File(
                decryptedFile,
                fileShare.File.ContentType,
                fileShare.File.OriginalFileName
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
            var fileShare = await _context.FileShares
                .Include(s => s.File)
                .FirstOrDefaultAsync(s => s.ShareToken == model.Token);

            if (fileShare == null)
            {
                model.ErrorMessage = "This share link is invalid or no longer exists.";

                return View("Password", model);
            }

            model.FileName = fileShare.File?.OriginalFileName;
            model.ContentType = fileShare.File?.ContentType;
            model.RequiresPassword = fileShare.IsPasswordProtected;

            if (fileShare.IsRevoked)
            {
                model.ErrorMessage = "This share link has been revoked.";

                return View("Password", model);
            }

            if (fileShare.ExpiresAt.HasValue &&
                fileShare.ExpiresAt.Value < DateTime.UtcNow)
            {
                model.ErrorMessage = "This share link has expired.";

                return View("Password", model);
            }

            if (fileShare.MaxDownloads.HasValue &&
                fileShare.DownloadCount >= fileShare.MaxDownloads.Value)
            {
                model.ErrorMessage = "This share link has reached its download limit.";

                return View("Password", model);
            }

            if (fileShare.File == null)
            {
                model.ErrorMessage = "The shared file could not be found.";

                return View("Password", model);
            }

            if (!System.IO.File.Exists(fileShare.File.FilePath))
            {
                model.ErrorMessage = "The shared file is no longer available.";

                return View("Password", model);
            }

            if (!fileShare.IsPasswordProtected)
            {
                byte[] decryptedFile = await _encryptionService.DecryptFileAsync(
                    fileShare.File.FilePath
                );

                fileShare.DownloadCount++;

                await _context.SaveChangesAsync();

                return File(
                    decryptedFile,
                    fileShare.File.ContentType,
                    fileShare.File.OriginalFileName
                );
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                model.ErrorMessage = "Please enter the password.";

                return View("Password", model);
            }

            if (string.IsNullOrEmpty(fileShare.PasswordHash))
            {
                model.ErrorMessage = "This share link is not configured correctly.";

                return View("Password", model);
            }

            var result = _passwordHasher.VerifyHashedPassword(
                fileShare,
                fileShare.PasswordHash,
                model.Password
            );

            if (result == PasswordVerificationResult.Failed)
            {
                model.ErrorMessage = "Incorrect password.";

                return View("Password", model);
            }

            byte[] decryptedPasswordProtectedFile =
                await _encryptionService.DecryptFileAsync(
                    fileShare.File.FilePath
                );

            fileShare.DownloadCount++;

            await _context.SaveChangesAsync();

            return File(
                decryptedPasswordProtectedFile,
                fileShare.File.ContentType,
                fileShare.File.OriginalFileName
            );
        }
    }
}
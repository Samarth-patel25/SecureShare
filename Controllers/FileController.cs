using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureShare.Data;
using SecureShare.Models;
using SecureShare.Services;

namespace SecureShare.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _encryptionService;
        public FileController(ApplicationDbContext context,EncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(FileUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.File.Length == 0)
            {
                ModelState.AddModelError("File", "File cannot be empty.");
                return View(model);
            }

            const long maxFileSize = 10 * 1024 * 1024;

            if (model.File.Length > maxFileSize)
            {
                ModelState.AddModelError("File", "File size cannot exceed 10 MB.");
                return View(model);
            }

            var allowedExtensions = new[]
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png",
                ".zip",
                ".docx"
            };

            string extension = Path.GetExtension(model.File.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("File", "This file type is not allowed.");
                return View(model);
            }

            string uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads"
            );

            Directory.CreateDirectory(uploadsFolder);

            string storedFileName = Guid.NewGuid().ToString() + extension;

            string filePath = Path.Combine(
                uploadsFolder,
                storedFileName
            );

            string tempFilePath = Path.Combine(
                uploadsFolder,
                "temp_" + Guid.NewGuid() + extension
            );

            using (var stream = new FileStream(
                tempFilePath,
                FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            await _encryptionService.EncryptFileAsync(
                tempFilePath,
                filePath
            );

            System.IO.File.Delete(tempFilePath);

            var file = new SecureShare.Models.File
            {
                OriginalFileName = model.File.FileName,
                StoredFileName = storedFileName,
                FilePath = filePath,
                FileSize = model.File.Length,
                ContentType = model.File.ContentType,
                UploadedAt = DateTime.UtcNow,
                UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            };

            _context.Files.Add(file);

            await _context.SaveChangesAsync();

            return RedirectToAction("MyFiles");
        }

        public async Task<IActionResult> MyFiles()
        {
            string userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )!.Value;

            var files = await _context.Files
                .Where(f => f.UserId == userId)
                .ToListAsync();

            return View(files);
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            string userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )!.Value;

            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
            {
                return NotFound();
            }

            bool isOwner = file.UserId == userId;

            bool hasPermission = await _context.FilePermissions
                .AnyAsync(p =>
                    p.FileId == id &&
                    p.UserId == userId &&
                    !p.IsRevoked);

            if (!isOwner && !hasPermission)
            {
                return Forbid();
            }

            if (!System.IO.File.Exists(file.FilePath))
            {
                return NotFound();
            }

            byte[] decryptedFile =
                await _encryptionService.DecryptFileAsync(
                    file.FilePath
                );

            return File(
                decryptedFile,
                file.ContentType,
                file.OriginalFileName
            );
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            string userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )!.Value;

            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (file == null)
            {
                return NotFound();
            }

            if (System.IO.File.Exists(file.FilePath))
            {
                System.IO.File.Delete(file.FilePath);
            }

            _context.Files.Remove(file);

            await _context.SaveChangesAsync();

            return RedirectToAction("MyFiles");
        }
        [Authorize]
        public async Task<IActionResult> SharedWithMe()
        {
            string userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )!.Value;

            var sharedFiles = await _context.FilePermissions
                .Include(p => p.File)
                .Where(p =>
                    p.UserId == userId &&
                    !p.IsRevoked)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(sharedFiles);
        }
    }
}
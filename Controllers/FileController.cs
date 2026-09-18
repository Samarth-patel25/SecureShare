using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureShare.Data;
using SecureShare.Models;

namespace SecureShare.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FileController(ApplicationDbContext context)
        {
            _context = context;
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

            string storedFileName = Guid.NewGuid().ToString() + extension;

            string filePath = Path.Combine(
                uploadsFolder,
                storedFileName
            );

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

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

            return Content("File uploaded successfully!");
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

        public async Task<IActionResult> Download(int id)
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

            if (!System.IO.File.Exists(file.FilePath))
            {
                return NotFound();
            }

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);

            return File(
                fileBytes,
                file.ContentType,
                file.OriginalFileName
            );
        }
    }
}
using System.ComponentModel.DataAnnotations;

namespace SecureShare.Models
{
    public class FileShare
    {
        public int Id { get; set; }

        [Required]
        public int FileId { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [Required]
        public string ShareToken { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        public bool IsRevoked { get; set; } = false;

        public int? MaxDownloads { get; set; }

        public int DownloadCount { get; set; } = 0;

        public File File { get; set; } = null!;
    }
}
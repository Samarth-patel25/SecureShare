using System.ComponentModel.DataAnnotations;

namespace SecureShare.Models
{
    public class File
    {
        public int Id { get; set; }

        [Required]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        public string StoredFileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string ContentType { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}
namespace SecureShare.Models
{
    public class FilePermission
    {
        public int Id { get; set; }

        public int FileId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string Permission { get; set; } = "Viewer";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRevoked { get; set; } = false;

        public File File { get; set; } = null!;
    }
}
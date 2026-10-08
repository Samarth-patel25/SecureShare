using System.ComponentModel.DataAnnotations;

namespace SecureShare.Models
{
    public class SharePasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        public bool RequiresPassword { get; set; }

        public string? FileName { get; set; }

        public string? ContentType { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
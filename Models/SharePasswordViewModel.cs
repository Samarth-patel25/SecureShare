using System.ComponentModel.DataAnnotations;

namespace SecureShare.Models
{
    public class SharePasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SecureShare.Models
{
    public class FileUploadViewModel
    {
        [Required]
        public IFormFile File { get; set; } = null!; //iFromfile represents the file that is being uploaded.
                                                     //It contains properties such as the file name, content type, and the actual file data.
    }
}
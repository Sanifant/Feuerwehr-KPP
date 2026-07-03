using System.ComponentModel.DataAnnotations;

namespace Feuerwehr.Common.Models.Auth
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}

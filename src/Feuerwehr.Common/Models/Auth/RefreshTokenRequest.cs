using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.Auth
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}

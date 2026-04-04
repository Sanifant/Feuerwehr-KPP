using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace de.openelp.authentification.Models;

public class RefreshToken
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    public string Token { get; set; } = string.Empty;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsRevoked { get; set; }
    
    public DateTime? RevokedAt { get; set; }
    
    public string? CreatedByIp { get; set; }
    
    public string? ReplacedByToken { get; set; } // Für Rotation
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
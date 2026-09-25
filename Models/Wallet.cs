using System.ComponentModel.DataAnnotations;

namespace canteen_management.Models;

public class Wallet
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    public User User { get; set; } = null!;

    [Range(0, double.MaxValue)]
    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

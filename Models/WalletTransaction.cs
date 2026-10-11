using System.ComponentModel.DataAnnotations;

namespace canteen_management.Models;

public class WalletTransaction
{
    public Guid Id { get; set; }

    public Guid WalletId { get; set; }

    [Required]
    public Wallet Wallet { get; set; } = null!;

    [Required]
    public WalletTransactionType Type { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

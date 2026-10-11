using System.ComponentModel.DataAnnotations;

namespace canteen_management.DTOs.Wallet;

public class TopUpWalletRequest
{
    [Required]
    [Range(1, 10000)]
    public decimal Amount { get; set; }

    public Guid TargetUserId { get; set; }
}

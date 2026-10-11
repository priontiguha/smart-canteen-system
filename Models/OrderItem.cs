using System.ComponentModel.DataAnnotations;

namespace canteen_management.Models;

public class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    [Required]
    public Order Order { get; set; } = null!;

    public Guid MenuItemId { get; set; }

    [Required]
    public MenuItem MenuItem { get; set; } = null!;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalPrice { get; set; }
}

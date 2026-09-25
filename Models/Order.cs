using System.ComponentModel.DataAnnotations;

namespace canteen_management.Models;

public class Order
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    public User User { get; set; } = null!;

    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

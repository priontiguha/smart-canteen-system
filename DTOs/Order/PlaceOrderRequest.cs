using System.ComponentModel.DataAnnotations;

namespace canteen_management.DTOs.Order;

public class PlaceOrderRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MinLength(1)]
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    [Required]
    public Guid MenuItemId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

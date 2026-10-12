using System.ComponentModel.DataAnnotations;

namespace canteen_management.Models;

public class GroupOrderItem
{
    public Guid Id { get; set; }

    public Guid GroupOrderId { get; set; }
    public GroupOrder? GroupOrder { get; set; }

    public Guid MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public Guid AddedByUserId { get; set; }
    public User? AddedByUser { get; set; }

    public int Quantity { get; set; }
}

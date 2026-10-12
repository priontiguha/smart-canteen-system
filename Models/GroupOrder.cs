using System.ComponentModel.DataAnnotations;

namespace canteen_management.Models;

public class GroupOrder
{
    public Guid Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public bool IsOpen { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupOrderItem> Items { get; set; } = new List<GroupOrderItem>();
}

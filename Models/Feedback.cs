using System.ComponentModel.DataAnnotations;

namespace canteen_management.Models;

public class Feedback
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    [Range(1,5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

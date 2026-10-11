using System.ComponentModel.DataAnnotations;
using canteen_management.Models;

namespace canteen_management.DTOs.Menu;

public class MenuItemRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public MealType MealType { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public bool IsAvailable { get; set; } = true;

    [StringLength(500)]
    public string? Description { get; set; }
}

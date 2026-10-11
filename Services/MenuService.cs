using canteen_management.Data;
using canteen_management.Models;
using Microsoft.EntityFrameworkCore;

namespace canteen_management.Services;

public class MenuService : IMenuService
{
    private readonly ApplicationDbContext _context;

    public MenuService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MenuItem>> GetAllAsync()
    {
        return await _context.MenuItems
            .OrderBy(x => x.MealType)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<MenuItem?> GetByIdAsync(Guid id)
    {
        return await _context.MenuItems.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<MenuItem> CreateAsync(MenuItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new InvalidOperationException("Menu item name is required.");

        if (item.Price <= 0)
            throw new InvalidOperationException("Price must be greater than zero.");

        item.Id = Guid.NewGuid();
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();

        return item;
    }

    public async Task<MenuItem> UpdateAsync(MenuItem item)
    {
        var existing = await _context.MenuItems.FirstOrDefaultAsync(x => x.Id == item.Id);
        if (existing is null)
            throw new InvalidOperationException("Menu item not found.");

        existing.Name = item.Name;
        existing.MealType = item.MealType;
        existing.Price = item.Price;
        existing.StockQuantity = item.StockQuantity;
        existing.IsAvailable = item.IsAvailable;
        existing.Description = item.Description;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return existing;
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.MenuItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null)
            throw new InvalidOperationException("Menu item not found.");

        _context.MenuItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    public async Task<List<MenuItem>> GetAvailableByMealAsync(MealType mealType)
    {
        return await _context.MenuItems
            .Where(x => x.MealType == mealType && x.IsAvailable && x.StockQuantity > 0)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<bool> ReduceStockAsync(Guid menuItemId, int quantity)
    {
        if (quantity <= 0)
            return false;

        var item = await _context.MenuItems.FirstOrDefaultAsync(x => x.Id == menuItemId);
        if (item is null)
            return false;

        if (item.StockQuantity < quantity)
            return false;

        item.StockQuantity -= quantity;
        item.IsAvailable = item.StockQuantity > 0;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}

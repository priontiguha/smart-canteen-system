using canteen_management.Models;

namespace canteen_management.Services;

public interface IMenuService
{
    Task<List<MenuItem>> GetAllAsync();
    Task<MenuItem?> GetByIdAsync(Guid id);
    Task<MenuItem> CreateAsync(MenuItem item);
    Task<MenuItem> UpdateAsync(MenuItem item);
    Task DeleteAsync(Guid id);
    Task<List<MenuItem>> GetAvailableByMealAsync(MealType mealType);
    Task<bool> ReduceStockAsync(Guid menuItemId, int quantity);
}

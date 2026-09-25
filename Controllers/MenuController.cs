using canteen_management.DTOs.Menu;
using canteen_management.Models;
using canteen_management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace canteen_management.Controllers;

[Authorize(AuthenticationSchemes = "Cookies", Roles = "Admin")]
public class MenuController : Controller
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var items = await _menuService.GetAllAsync();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new MenuItemRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuItemRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var item = new MenuItem
            {
                Name = model.Name.Trim(),
                MealType = model.MealType,
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                IsAvailable = model.IsAvailable,
                Description = model.Description
            };

            await _menuService.CreateAsync(item);
            TempData["SuccessMessage"] = "Menu item created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _menuService.GetByIdAsync(id);
        if (item is null)
            return NotFound();

        var model = new MenuItemRequest
        {
            Name = item.Name,
            MealType = item.MealType,
            Price = item.Price,
            StockQuantity = item.StockQuantity,
            IsAvailable = item.IsAvailable,
            Description = item.Description
        };

        ViewBag.MenuItemId = item.Id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MenuItemRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var existing = await _menuService.GetByIdAsync(id);
            if (existing is null)
                return NotFound();

            existing.Name = model.Name.Trim();
            existing.MealType = model.MealType;
            existing.Price = model.Price;
            existing.StockQuantity = model.StockQuantity;
            existing.IsAvailable = model.IsAvailable;
            existing.Description = model.Description;

            await _menuService.UpdateAsync(existing);
            TempData["SuccessMessage"] = "Menu item updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _menuService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Menu item deleted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "This menu item cannot be deleted because it is already linked to existing orders.";
        }

        return RedirectToAction(nameof(Index));
    }
}

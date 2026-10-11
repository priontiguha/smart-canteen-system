using canteen_management.DTOs.Order;
using canteen_management.Models;
using canteen_management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace canteen_management.Controllers;

[Authorize(AuthenticationSchemes = "Cookies")]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IMenuService _menuService;
    private readonly IWalletService _walletService;

    public OrderController(IOrderService orderService, IMenuService menuService, IWalletService walletService)
    {
        _orderService = orderService;
        _menuService = menuService;
        _walletService = walletService;
    }

    [HttpGet]
    public async Task<IActionResult> Menu()
    {
        var breakfast = await _menuService.GetAvailableByMealAsync(MealType.Breakfast);
        var lunch = await _menuService.GetAvailableByMealAsync(MealType.Lunch);
        var dinner = await _menuService.GetAvailableByMealAsync(MealType.Dinner);

        ViewBag.Breakfast = breakfast;
        ViewBag.Lunch = lunch;
        ViewBag.Dinner = dinner;

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            ViewBag.WalletBalance = await _walletService.GetBalanceAsync(userId);
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder([FromForm] PlaceOrderRequest request)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Menu));

        try
        {
            var order = await _orderService.PlaceOrderAsync(request);
            TempData["OrderSuccess"] = $"Order placed successfully. Total: ${order.TotalAmount:F2}";
            return RedirectToAction(nameof(MyOrders));
        }
        catch (InvalidOperationException ex)
        {
            TempData["OrderError"] = ex.Message;
            return RedirectToAction(nameof(Menu));
        }
    }

    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account");

        var orders = await _orderService.GetOrdersForUserAsync(userId);
        return View(orders);
    }

    [Authorize(AuthenticationSchemes = "Cookies", Roles = "KitchenStaff,Admin")]
    [HttpGet]
    public async Task<IActionResult> KitchenQueue()
    {
        var orders = await _orderService.GetPendingOrdersAsync();
        return View(orders);
    }

    [Authorize(AuthenticationSchemes = "Cookies", Roles = "KitchenStaff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, OrderStatus status)
    {
        try
        {
            await _orderService.UpdateStatusAsync(id, status);
            TempData["KitchenSuccess"] = "Order status updated.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["KitchenError"] = ex.Message;
        }

        return RedirectToAction(nameof(KitchenQueue));
    }
}

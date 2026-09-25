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
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account");

        request.UserId = userId;

        var parsedItems = new List<canteen_management.DTOs.Order.OrderItemRequest>();
        var formKeys = Request.Form.Keys
            .Where(key => key.StartsWith("Items[") && key.Contains("]") && key.EndsWith("] .MenuItemId") == false)
            .ToList();

        var selectedIds = Request.Form
            .Where(kvp => kvp.Key.StartsWith("Items[") && kvp.Key.Contains("].MenuItemId"))
            .Select(kvp => kvp.Key)
            .ToList();

        if (selectedIds.Count == 0 && request.Items != null)
            request.Items = request.Items.Where(x => x != null && x.Quantity > 0).ToList();

        if (selectedIds.Count > 0)
        {
            var keyedItems = Request.Form
                .Where(kvp => kvp.Key.StartsWith("Items[") && kvp.Key.Contains("].MenuItemId"))
                .Select(kvp => new
                {
                    Key = kvp.Key,
                    MenuItemId = kvp.Value
                })
                .ToList();

            foreach (var menuItem in keyedItems)
            {
                var index = menuItem.Key.Split('[', ']')[1];
                var quantityValue = Request.Form[$"Items[{index}].Quantity"];
                if (!int.TryParse(quantityValue, out var quantity) || quantity <= 0)
                    continue;

                if (Guid.TryParse(menuItem.MenuItemId, out var menuId))
                    parsedItems.Add(new canteen_management.DTOs.Order.OrderItemRequest
                    {
                        MenuItemId = menuId,
                        Quantity = quantity
                    });
            }

            request.Items = parsedItems;
        }

        request.Items = request.Items?.Where(x => x != null && x.Quantity > 0).ToList() ?? new List<canteen_management.DTOs.Order.OrderItemRequest>();

        if (!ModelState.IsValid || request.Items.Count == 0)
        {
            TempData["OrderError"] = "Please select at least one item before placing the order.";
            return RedirectToAction(nameof(Menu));
        }

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

    [Authorize(AuthenticationSchemes = "Cookies")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDelivered(Guid id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account");

        try
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order is null)
                throw new InvalidOperationException("Order not found.");

            if (order.UserId != userId)
                throw new InvalidOperationException("You can only update your own orders.");

            if (order.Status != OrderStatus.ReadyForPickup)
                throw new InvalidOperationException("This order is not ready for pickup yet.");

            await _orderService.UpdateStatusAsync(id, OrderStatus.Delivered);
            TempData["OrderSuccess"] = "Food delivered successfully. Enjoy your meal!";
        }
        catch (InvalidOperationException ex)
        {
            TempData["OrderError"] = ex.Message;
        }

        return RedirectToAction(nameof(MyOrders));
    }
}

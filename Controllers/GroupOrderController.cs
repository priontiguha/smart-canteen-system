using canteen_management.Data;
using canteen_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace canteen_management.Controllers;

[Authorize(AuthenticationSchemes = "Cookies")]
public class GroupOrderController : Controller
{
    private readonly ApplicationDbContext _context;

    public GroupOrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var list = await _context.GroupOrders
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id)
    {
        var group = await _context.GroupOrders.FindAsync(id);
        if (group is null) return NotFound();

        // Close and convert into a real Order where the owner pays for aggregated items.
        var items = await _context.GroupOrderItems
            .Where(x => x.GroupOrderId == id)
            .Include(x => x.MenuItem)
            .ToListAsync();

        if (items == null || items.Count == 0)
        {
            // nothing to convert
            group.IsOpen = false;
            group.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // Aggregate into order items
            var orderItems = new List<OrderItem>();
            decimal total = 0m;
            foreach (var gi in items)
            {
                var price = gi.MenuItem?.Price ?? 0m;
                var lineTotal = price * gi.Quantity;
                total += lineTotal;
                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = gi.MenuItemId,
                    Quantity = gi.Quantity,
                    UnitPrice = price,
                    TotalPrice = lineTotal
                });
            }

            // Owner will be charged
            var owner = await _context.Users.FindAsync(group.OwnerId);
            if (owner is null)
                throw new InvalidOperationException("Group owner not found.");

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == owner.Id);
            if (wallet == null || wallet.Balance < total)
            {
                // insufficient funds: abort conversion and mark group closed
                group.IsOpen = false;
                group.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["GroupError"] = "Owner has insufficient balance; group closed without payment.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Deduct owner wallet and reduce stock
            wallet.Balance -= total;
            _context.WalletTransactions.Add(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = -total,
                Description = $"Group order payment for '{group.Title}'",
                CreatedAt = DateTime.UtcNow
            });

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = owner.Id,
                Status = OrderStatus.Pending,
                TotalAmount = total,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var oi in orderItems)
            {
                oi.OrderId = order.Id;
                oi.Order = order;
                // reduce stock
                var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == oi.MenuItemId);
                if (menuItem == null || menuItem.StockQuantity < oi.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for item {oi.MenuItemId}");
                }
                menuItem.StockQuantity -= oi.Quantity;
                menuItem.IsAvailable = menuItem.StockQuantity > 0;
                menuItem.UpdatedAt = DateTime.UtcNow;
            }

            order.Items = orderItems;
            _context.Orders.Add(order);

            // finalize group
            group.IsOpen = false;
            group.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["GroupSuccess"] = "Group order converted and charged to owner.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            TempData["GroupError"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
    public IActionResult Create()
    {
        return View(new GroupOrder());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GroupOrder model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account");

        model.Id = Guid.NewGuid();
        model.OwnerId = userId;
        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;

        _context.GroupOrders.Add(model);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var group = await _context.GroupOrders
            .FindAsync(id);

        if (group is null)
            return NotFound();

        var items = await _context.GroupOrderItems
            .Where(x => x.GroupOrderId == id)
            .Include(x => x.MenuItem)
            .Include(x => x.AddedByUser)
            .ToListAsync();

        ViewBag.MenuItems = await _context.MenuItems.Where(m => m.IsAvailable).ToListAsync();

        ViewBag.Items = items;
        return View(group);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(Guid groupId, Guid menuItemId, int quantity)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account");

        var group = await _context.GroupOrders.FindAsync(groupId);
        if (group is null || !group.IsOpen)
            return BadRequest("Group order not found or closed.");

        var menuItem = await _context.MenuItems.FindAsync(menuItemId);
        if (menuItem is null)
            return BadRequest("Menu item not found.");

        var item = new GroupOrderItem
        {
            Id = Guid.NewGuid(),
            GroupOrderId = groupId,
            MenuItemId = menuItemId,
            AddedByUserId = userId,
            Quantity = Math.Max(1, quantity)
        };

        _context.GroupOrderItems.Add(item);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = groupId });
    }
}

using canteen_management.Data;
using canteen_management.DTOs.Order;
using canteen_management.Models;
using Microsoft.EntityFrameworkCore;

namespace canteen_management.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IWalletService _walletService;
    private readonly IMenuService _menuService;

    public OrderService(ApplicationDbContext context, IWalletService walletService, IMenuService menuService)
    {
        _context = context;
        _walletService = walletService;
        _menuService = menuService;
    }

    public async Task<Order> PlaceOrderAsync(PlaceOrderRequest request)
    {
        if (request.Items == null || !request.Items.Any())
            throw new InvalidOperationException("Order must contain at least one item.");

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.UserId);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        var orderItems = new List<OrderItem>();
        decimal total = 0m;

        foreach (var itemRequest in request.Items)
        {
            var menuItem = await _context.MenuItems.FirstOrDefaultAsync(x => x.Id == itemRequest.MenuItemId);
            if (menuItem is null)
                throw new InvalidOperationException("One or more menu items were not found.");

            if (!menuItem.IsAvailable || menuItem.StockQuantity < itemRequest.Quantity)
                throw new InvalidOperationException($"Item '{menuItem.Name}' is not available in the required quantity.");

            var lineTotal = menuItem.Price * itemRequest.Quantity;
            total += lineTotal;

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                MenuItemId = menuItem.Id,
                MenuItem = menuItem,
                Quantity = itemRequest.Quantity,
                UnitPrice = menuItem.Price,
                TotalPrice = lineTotal
            });
        }

        var walletBalance = await _walletService.GetBalanceAsync(user.Id);
        if (walletBalance < total)
            throw new InvalidOperationException("Insufficient wallet balance.");

        var walletDeducted = await _walletService.DeductAsync(user.Id, total, "Order payment");
        if (!walletDeducted)
            throw new InvalidOperationException("Wallet deduction failed.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Status = OrderStatus.Pending,
            TotalAmount = total,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var item in orderItems)
        {
            item.Order = order;
            item.OrderId = order.Id;
            await _menuService.ReduceStockAsync(item.MenuItemId, item.Quantity);
        }

        order.Items = orderItems;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }

    public async Task<List<Order>> GetOrdersForUserAsync(Guid userId)
    {
        return await _context.Orders
            .Include(x => x.Items)
            .ThenInclude(x => x.MenuItem)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Order>> GetPendingOrdersAsync()
    {
        return await _context.Orders
            .Include(x => x.Items)
            .ThenInclude(x => x.MenuItem)
            .Where(x => x.Status != OrderStatus.ReadyForPickup)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(x => x.Items)
            .ThenInclude(x => x.MenuItem)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task UpdateStatusAsync(Guid orderId, OrderStatus status)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
        if (order is null)
            throw new InvalidOperationException("Order not found.");

        if (order.Status == OrderStatus.ReadyForPickup && status != OrderStatus.ReadyForPickup)
            throw new InvalidOperationException("Order is already completed.");

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

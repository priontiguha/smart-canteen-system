using canteen_management.DTOs.Order;
using canteen_management.Models;

namespace canteen_management.Services;

public interface IOrderService
{
    Task<Order> PlaceOrderAsync(PlaceOrderRequest request);
    Task<List<Order>> GetOrdersForUserAsync(Guid userId);
    Task<List<Order>> GetPendingOrdersAsync();
    Task<Order?> GetByIdAsync(Guid id);
    Task UpdateStatusAsync(Guid orderId, OrderStatus status);
}

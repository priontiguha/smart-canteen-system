using canteen_management.Data;
using canteen_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace canteen_management.Controllers;

[Authorize(AuthenticationSchemes = "Cookies")]
public class FeedbackController : Controller
{
    private readonly ApplicationDbContext _context;

    public FeedbackController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Create(Guid orderId)
    {
        ViewBag.OrderId = orderId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int rating, string comments, Guid orderId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account");

        var fb = new Feedback
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderId = orderId,
            Rating = rating,
            Comments = comments,
            CreatedAt = DateTime.UtcNow
        };

        _context.Feedbacks.Add(fb);
        await _context.SaveChangesAsync();

        TempData["OrderSuccess"] = "Thanks for your feedback!";
        return RedirectToAction("MyOrders", "Order");
    }
}

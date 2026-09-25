using System.Security.Claims;
using canteen_management.Data;
using canteen_management.DTOs.Auth;
using canteen_management.Models;
using canteen_management.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace canteen_management.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;

    public AccountController(IAuthService authService, ApplicationDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var user = await _authService.LoginAsync(model);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString()),
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);

            return RedirectToAction("Dashboard");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromForm] RegisterRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _authService.RegisterAsync(model);
            TempData["SuccessMessage"] = "Registration successful. Please log in.";
            return RedirectToAction("Login");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Authorize(AuthenticationSchemes = "Cookies", Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser([FromForm] AdminCreateUserRequest model)
    {
        if (!ModelState.IsValid)
        {
            var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var currentEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
            var currentRole = User.FindFirstValue(ClaimTypes.Role) ?? UserRole.Student.ToString();
            ViewBag.CurrentUserName = currentUser;
            ViewBag.CurrentUserEmail = currentEmail;
            ViewBag.CurrentUserRole = currentRole;
            return View("Dashboard", new { Name = currentUser, Email = currentEmail, Role = currentRole });
        }

        try
        {
            await _authService.CreateUserByAdminAsync(model);
            TempData["AdminSuccessMessage"] = "User created successfully.";
            return RedirectToAction("Dashboard");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var currentEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
            var currentRole = User.FindFirstValue(ClaimTypes.Role) ?? UserRole.Student.ToString();
            ViewBag.CurrentUserName = currentUser;
            ViewBag.CurrentUserEmail = currentEmail;
            ViewBag.CurrentUserRole = currentRole;
            return View("Dashboard", new { Name = currentUser, Email = currentEmail, Role = currentRole });
        }
    }

    [Authorize(AuthenticationSchemes = "Cookies")]
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var name = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        var role = User.FindFirstValue(ClaimTypes.Role) ?? UserRole.Student.ToString();
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? currentUserId = Guid.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

        var isStudent = User.IsInRole(UserRole.Student.ToString());
        var isAdminUser = User.IsInRole(UserRole.Admin.ToString());

        var menuCount = await _context.MenuItems.CountAsync();
        var lowStockCount = await _context.MenuItems.CountAsync(x => x.StockQuantity <= 5);
        var revenue = await _context.Orders.SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

        IQueryable<Order> recentOrdersQuery = _context.Orders
            .Include(x => x.User)
            .AsNoTracking();

        if (isStudent)
        {
            if (currentUserId.HasValue)
            {
                recentOrdersQuery = recentOrdersQuery.Where(x => x.UserId == currentUserId.Value);
            }
            else
            {
                recentOrdersQuery = recentOrdersQuery.Where(x => false);
            }
        }

        var orderCount = isStudent && currentUserId.HasValue
            ? await _context.Orders.CountAsync(x => x.UserId == currentUserId.Value)
            : await _context.Orders.CountAsync();

        if (isStudent)
        {
            menuCount = 0;
            lowStockCount = 0;
            revenue = 0m;
        }

        var recentOrders = await recentOrdersQuery
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.IsStudent = isStudent;
        ViewBag.IsAdmin = isAdminUser;
        ViewBag.MenuCount = menuCount;
        ViewBag.OrderCount = orderCount;
        ViewBag.LowStockCount = lowStockCount;
        ViewBag.Revenue = revenue;
        ViewBag.RecentOrders = recentOrders;

        return View(new { Name = name, Email = email, Role = role });
    }

    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("canteen_auth");
        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        TempData.Clear();
        return Redirect("/Account/Login");
    }

    [HttpGet("Logout")]
    public async Task<IActionResult> LogoutGet()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("canteen_auth");
        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        TempData.Clear();
        return Redirect("/Account/Login");
    }
}

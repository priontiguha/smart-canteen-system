using System.Security.Claims;
using canteen_management.DTOs.Auth;
using canteen_management.Models;
using canteen_management.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace canteen_management.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
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
    public IActionResult Dashboard()
    {
        var name = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        var role = User.FindFirstValue(ClaimTypes.Role) ?? UserRole.Student.ToString();

        return View(new { Name = name, Email = email, Role = role });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }

    [HttpGet("/Account/Logout")]
    public async Task<IActionResult> LogoutGet()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }
}

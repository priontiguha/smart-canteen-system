using canteen_management.Data;
using canteen_management.DTOs.Auth;
using canteen_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace canteen_management.Controllers;

[Authorize(AuthenticationSchemes = "Cookies", Roles = "Admin")]
public class UserManagementController : Controller
{
    private readonly ApplicationDbContext _context;

    public UserManagementController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users
            .OrderBy(x => x.FullName)
            .ToListAsync();

        return View(users);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new AdminCreateUserRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCreateUserRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var existingUser = await _context.Users
            .AnyAsync(x => x.Email == model.Email.Trim().ToLowerInvariant());

        if (existingUser)
        {
            ModelState.AddModelError(string.Empty, "An account with this email already exists.");
            return View(model);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Role = model.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.Wallets.Add(new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Balance = 0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "User created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
            return NotFound();

        var model = new EditUserRequest
        {
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive
        };

        ViewBag.UserId = user.Id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditUserRequest model)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        var duplicate = await _context.Users
            .AnyAsync(x => x.Id != id && x.Email == model.Email.Trim().ToLowerInvariant());

        if (duplicate)
        {
            ModelState.AddModelError(string.Empty, "Another user already exists with this email.");
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim().ToLowerInvariant();
        user.Role = model.Role;
        user.IsActive = model.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "User updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
            return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "User deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}

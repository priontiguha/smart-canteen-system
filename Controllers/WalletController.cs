using canteen_management.Data;
using canteen_management.DTOs.Wallet;
using canteen_management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace canteen_management.Controllers;

[Authorize(AuthenticationSchemes = "Cookies")]
public class WalletController : Controller
{
    private readonly IWalletService _walletService;
    private readonly ApplicationDbContext _context;

    public WalletController(IWalletService walletService, ApplicationDbContext context)
    {
        _walletService = walletService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account");

        var balance = await _walletService.GetBalanceAsync(userId);
        var transactions = await _walletService.GetTransactionsAsync(userId);

        ViewBag.Balance = balance;
        ViewBag.Transactions = transactions;
        ViewBag.IsAdmin = User.IsInRole("Admin");

        if (User.IsInRole("Admin"))
        {
            ViewBag.Users = await _context.Users
                .OrderBy(x => x.FullName)
                .Select(x => new
                {
                    x.Id,
                    x.FullName,
                    x.Email,
                    x.Role
                })
                .ToListAsync();
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TopUp(TopUpWalletRequest model)
    {
        if (!User.IsInRole("Admin"))
        {
            TempData["WalletError"] = "Only administrators can top up wallets.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid || model.TargetUserId == Guid.Empty)
        {
            TempData["WalletError"] = "Select a user and enter a valid amount.";
            return RedirectToAction(nameof(Index));
        }

        var targetUser = await _context.Users.FirstOrDefaultAsync(x => x.Id == model.TargetUserId);
        if (targetUser is null)
        {
            TempData["WalletError"] = "Selected user was not found.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _walletService.AddAsync(targetUser.Id, model.Amount, $"Admin wallet top-up for {targetUser.FullName}");
            TempData["WalletSuccess"] = $"Wallet topped up successfully for {targetUser.FullName}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["WalletError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}

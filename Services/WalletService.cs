using canteen_management.Data;
using canteen_management.Models;
using Microsoft.EntityFrameworkCore;

namespace canteen_management.Services;

public class WalletService : IWalletService
{
    private readonly ApplicationDbContext _context;

    public WalletService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetBalanceAsync(Guid userId)
    {
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(x => x.UserId == userId);

        return wallet?.Balance ?? 0m;
    }

    public async Task<bool> DeductAsync(Guid userId, decimal amount, string description)
    {
        if (amount <= 0)
            return false;

        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (wallet is null)
            return false;

        if (wallet.Balance < amount)
            return false;

        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        _context.WalletTransactions.Add(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Wallet = wallet,
            Type = WalletTransactionType.Debit,
            Amount = amount,
            Description = description,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task AddAsync(Guid userId, decimal amount, string description)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero.");

        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (wallet is null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null)
                throw new InvalidOperationException("User not found.");

            wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                Balance = 0m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Wallets.Add(wallet);
        }

        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        _context.WalletTransactions.Add(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Wallet = wallet,
            Type = WalletTransactionType.Credit,
            Amount = amount,
            Description = description,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task<List<object>> GetTransactionsAsync(Guid userId)
    {
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (wallet is null)
            return new List<object>();

        return await _context.WalletTransactions
            .Where(x => x.WalletId == wallet.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Type,
                x.Amount,
                x.Description,
                x.CreatedAt
            })
            .Cast<object>()
            .ToListAsync();
    }
}

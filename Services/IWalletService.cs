namespace canteen_management.Services;

public interface IWalletService
{
    Task<decimal> GetBalanceAsync(Guid userId);
    Task<bool> DeductAsync(Guid userId, decimal amount, string description);
    Task AddAsync(Guid userId, decimal amount, string description);
    Task<List<object>> GetTransactionsAsync(Guid userId);
}

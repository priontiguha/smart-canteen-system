using canteen_management.DTOs.Auth;
using canteen_management.Models;

namespace canteen_management.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<User> CreateUserByAdminAsync(AdminCreateUserRequest request, CancellationToken cancellationToken = default);
    Task<User> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
}

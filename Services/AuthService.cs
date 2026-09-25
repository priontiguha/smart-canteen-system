using canteen_management.Data;
using canteen_management.DTOs.Auth;
using canteen_management.Models;
using Microsoft.EntityFrameworkCore;

namespace canteen_management.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(ApplicationDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<User> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new InvalidOperationException("Full name is required.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Password is required.");

        var normalizedEmail = request.Email.Trim();
        if (!IsValidEmail(normalizedEmail))
            throw new InvalidOperationException("Invalid email format.");

        if (!IsGmailEmail(normalizedEmail))
            throw new InvalidOperationException("Only Gmail addresses are allowed.");

        if (!IsStrongPassword(request.Password))
            throw new InvalidOperationException("Password must contain at least 8 characters, one uppercase letter, one lowercase letter, one number, and one special character.");

        if (request.Password != request.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match.");

        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail.ToLowerInvariant(), cancellationToken);

        if (existingUser is not null)
            throw new InvalidOperationException("An account with this email already exists.");

        var hasAnyUsers = await _context.Users.AnyAsync(cancellationToken);
        var assignedRole = hasAnyUsers ? UserRole.Student : UserRole.Admin;

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = normalizedEmail.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = assignedRole,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<User> CreateUserByAdminAsync(AdminCreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new InvalidOperationException("Full name is required.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Password is required.");

        var normalizedEmail = request.Email.Trim();
        if (!IsValidEmail(normalizedEmail))
            throw new InvalidOperationException("Invalid email format.");

        if (!IsGmailEmail(normalizedEmail))
            throw new InvalidOperationException("Only Gmail addresses are allowed.");

        if (!IsAllowedRole(request.Role))
            throw new InvalidOperationException("Invalid role selected.");

        if (!IsStrongPassword(request.Password))
            throw new InvalidOperationException("Password must contain at least 8 characters, one uppercase letter, one lowercase letter, one number, and one special character.");

        if (request.Password != request.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match.");

        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail.ToLowerInvariant(), cancellationToken);

        if (existingUser is not null)
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = normalizedEmail.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<User> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Password is required.");

        var email = request.Email.Trim();
        if (!IsValidEmail(email))
            throw new InvalidOperationException("Invalid email format.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid email or password.");

        if (!user.IsActive)
            throw new InvalidOperationException("This account is inactive.");

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), cancellationToken);
    }

    private static bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
               email.Contains('@') &&
               email.Split('@').Length == 2 &&
               !email.StartsWith('@') &&
               !email.EndsWith('@');
    }

    private static bool IsGmailEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalized = email.Trim();
        return normalized.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowedRole(UserRole role)
    {
        return role == UserRole.Student || role == UserRole.Faculty || role == UserRole.KitchenStaff;
    }

    private static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        return password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(ch => !char.IsLetterOrDigit(ch));
    }
}

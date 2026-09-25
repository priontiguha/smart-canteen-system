using canteen_management.Models;

namespace canteen_management.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}

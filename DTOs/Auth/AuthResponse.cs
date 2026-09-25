namespace canteen_management.DTOs.Auth;

public class AuthResponse
{
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public UserSummary User { get; set; } = new();
}

public class UserSummary
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

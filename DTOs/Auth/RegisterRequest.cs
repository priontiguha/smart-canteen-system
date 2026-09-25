using System.ComponentModel.DataAnnotations;

namespace canteen_management.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [MinLength(2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [GmailDomain]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

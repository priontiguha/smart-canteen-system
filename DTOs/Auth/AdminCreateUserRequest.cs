using System.ComponentModel.DataAnnotations;
using canteen_management.Models;

namespace canteen_management.DTOs.Auth;

public class AdminCreateUserRequest
{
    [Required]
    [MinLength(2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [GmailDomain]
    public string Email { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Student;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

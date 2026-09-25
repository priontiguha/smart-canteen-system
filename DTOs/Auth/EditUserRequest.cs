using System.ComponentModel.DataAnnotations;
using canteen_management.Models;

namespace canteen_management.DTOs.Auth;

public class EditUserRequest
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

    public string? Password { get; set; }

    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }

    public bool IsActive { get; set; } = true;
}

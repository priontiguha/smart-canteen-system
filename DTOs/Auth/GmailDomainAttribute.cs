using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace canteen_management.DTOs.Auth;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class GmailDomainAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string email || string.IsNullOrWhiteSpace(email))
            return false;

        var trimmedEmail = email.Trim();

        if (!MailAddress.TryCreate(trimmedEmail, out var mailAddress))
            return false;

        return string.Equals(mailAddress.Host, "gmail.com", StringComparison.OrdinalIgnoreCase);
    }

    public override string FormatErrorMessage(string name)
    {
        return "Only Gmail addresses are allowed.";
    }
}

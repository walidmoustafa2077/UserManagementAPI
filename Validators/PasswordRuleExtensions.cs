using FluentValidation;

namespace UserManagementApi.Validators;

public static class PasswordRuleExtensions
{
    // Strong password: length >= 8, at least one upper, lower, digit, special
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^\w\s]").WithMessage("Password must contain at least one special character.");
}

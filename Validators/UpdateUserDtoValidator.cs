using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs;

namespace UserManagementApi.Validators;

/// <summary>
/// Validates partial updates. Uses RootContextData["RouteId"] (set by our filter)
/// to ensure email uniqueness excludes the current user.
/// </summary>
public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    private readonly AppDbContext _db;

    public UpdateUserDtoValidator(AppDbContext db)
    {
        _db = db;

        When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
        {
            RuleFor(x => x.Name!)
                .MinimumLength(2).WithMessage("Name must be at least 2 characters.")
                .MaximumLength(100).WithMessage("Name must be at most 100 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email!)
                .EmailAddress().WithMessage("Email is invalid.")
                .MustAsync(BeUniqueEmailForUpdate).WithMessage("Email is already in use.")
                .WithErrorCode("DuplicateEmail");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Password), () =>
        {
            RuleFor(x => x.Password!)
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"\d").WithMessage("Password must contain at least one digit.")
                .Matches(@"[^\w\s]").WithMessage("Password must contain at least one special character.");
        });
    }

    private async Task<bool> BeUniqueEmailForUpdate(UpdateUserDto dto, string email, ValidationContext<UpdateUserDto> ctx, CancellationToken ct)
    {
        // Route id is injected by our endpoint filter
        var hasId = ctx.RootContextData.TryGetValue("RouteId", out var idObj);
        var currentId = hasId && idObj is int i ? i : 0;

        var normalized = email.Trim().ToLowerInvariant();
        return !await _db.Users.AnyAsync(u => u.Email.ToLower() == normalized && u.Id != currentId, ct);
    }
}

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs;

namespace UserManagementApi.Validators;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    private readonly AppDbContext _db;

    public CreateUserDtoValidator(AppDbContext db)
    {
        _db = db;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Name must be at most 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is invalid.")
            .MustAsync(BeUniqueEmail).WithMessage("Email is already in use.")
            .WithErrorCode("DuplicateEmail");

        RuleFor(x => x.Password)
            .StrongPassword();

    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;
        var normalized = email.Trim().ToLowerInvariant();
        return !await _db.Users.AnyAsync(u => u.Email.ToLower() == normalized, ct);
    }
}

using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace UserManagementApi.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T>? _validator;

    public ValidationFilter(IValidator<T>? validator) => _validator = validator;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (_validator is null)
            return await next(context); // No validator registered for T

        var arg = context.Arguments.FirstOrDefault(a => a is T) as T;
        if (arg is null)
            return await next(context); // No matching argument to validate

        var fvCtx = new ValidationContext<T>(arg);
        // Attach route id (used by Update validator for uniqueness)
        if (context.HttpContext.Request.RouteValues.TryGetValue("id", out var idObj) &&
            idObj is string s && int.TryParse(s, out var id))
        {
            fvCtx.RootContextData["RouteId"] = id;
        }

        ValidationResult result = await _validator.ValidateAsync(fvCtx);
        if (result.IsValid)
            return await next(context);

        if (result.Errors.Any(e => e.ErrorCode == "DuplicateEmail"))
        {
            var pd = new ProblemDetails
            {
                Title = "Duplicate email",
                Detail = "Email is already in use.",
                Status = StatusCodes.Status409Conflict,
                Type = "about:blank"
            };
            return Results.Conflict(pd);
        }

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors); // 400 for all other cases
    }
}

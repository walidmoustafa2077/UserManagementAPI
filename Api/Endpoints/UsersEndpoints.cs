using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UserManagementApi.Api.OpenApi;
using UserManagementApi.Data;
using UserManagementApi.DTOs;
using UserManagementApi.Filters;
using UserManagementApi.Models;

namespace UserManagementApi.Api.Endpoints;

/// <summary>
/// Users endpoints group.
/// </summary>
public static class UsersEndpoints
{
    /// <summary>
    /// Maps the users endpoints.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The route group builder.</returns>
    public static RouteGroupBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        // Users group
        var group = app.MapGroup("/users")
                       .WithTags("Users")
                       .RequireAuthorization();

        // GET: all users
        group.MapGet("/", async (AppDbContext db) =>
        {
            try
            {
                var users = await db.Users.AsNoTracking()
                    .Select(u => new UserDto(u.Id, u.Name, u.Email, u.CreatedAt))
                    .ToListAsync();

                return Results.Ok(users);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching users: {ex.Message}");
                return Results.Problem("An error occurred while processing your request.", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("ListUsers")
        .WithSummary("List users")
        .WithDescription("Returns all users as an array. Returns 200 OK with an empty array when no users exist.")
        .Produces<List<UserDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithOpenApi(op =>
        {
            if (op.Responses.TryGetValue("200", out var ok))
            {
                var mediaType = ok.Content.TryGetValue("application/json", out var mt)
                    ? mt
                    : (ok.Content["application/json"] = new OpenApiMediaType());

                mediaType.Example = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["id"] = new OpenApiInteger(1),
                        ["name"] = new OpenApiString("User"),
                        ["email"] = new OpenApiString("user@techhive.com"),
                        ["createdAt"] = new OpenApiString("2025-08-12T00:00:00Z")
                    },
                    new OpenApiObject
                    {
                        ["id"] = new OpenApiInteger(2),
                        ["name"] = new OpenApiString("User2"),
                        ["email"] = new OpenApiString("user2@techhive.com"),
                        ["createdAt"] = new OpenApiString("2025-08-13T00:00:00Z")
                    },
                };
            }
            return op;
        });

        // GET: user by id
        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            try
            {
                var user = await db.Users.AsNoTracking()
                    .Where(u => u.Id == id)
                    .Select(u => new UserDto(u.Id, u.Name, u.Email, u.CreatedAt))
                    .FirstOrDefaultAsync();

                return user is not null ? Results.Ok(user) : Results.NotFound();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching user: {ex.Message}");
                return Results.Problem("An error occurred while processing your request.", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetUser")
        .WithSummary("Get a user")
        .WithDescription("Returns a single user by id.")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<UserDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi(op =>
        {
            if (op.Responses.TryGetValue("200", out var ok))
            {
                ok.Content ??= new Dictionary<string, OpenApiMediaType>();
                ok.Content["application/json"] = new OpenApiMediaType
                {
                    Example = new OpenApiObject
                    {
                        ["id"] = new OpenApiInteger(2),
                        ["name"] = new OpenApiString("Noura"),
                        ["email"] = new OpenApiString("noura@example.com"),
                        ["createdAt"] = new OpenApiString("2025-08-12T05:21:07Z")
                    }
                };
            }

            OpenApiDocs.Ensure404(op, "User with id 42 was not found.");
            return op;
        });

        // POST: create user
        group.MapPost("/", async (CreateUserDto input, AppDbContext db) =>
        {
            try
            {
                var normalized = input.Email.Trim().ToLowerInvariant();
                var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == normalized);
                if (exists)
                    return Results.Conflict(new ProblemDetails
                    { Title = "Duplicate email", Detail = "Email is already in use." });

                var entity = new User
                {
                    Name = input.Name.Trim(),
                    Email = input.Email.Trim(),
                    Password = input.Password // hash in real apps
                };

                db.Users.Add(entity);
                await db.SaveChangesAsync();

                return Results.Created($"/users/{entity.Id}", ToDto(entity));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating user: {ex.Message}");
                return Results.Problem("An error occurred while processing your request.", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("CreateUser")
        .WithSummary("Create a user")
        .WithDescription("Creates a new user. All fields are required.")
        .Accepts<CreateUserDto>("application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<UserDto>(StatusCodes.Status201Created)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .AddEndpointFilter<ValidationFilter<CreateUserDto>>()
        .WithOpenApi(op =>
        {
            op.Responses.Remove("200");

            if (op.Responses.TryGetValue("201", out var created))
            {
                created.Content ??= new Dictionary<string, OpenApiMediaType>();
                created.Content["application/json"] = new OpenApiMediaType
                {
                    Example = new OpenApiObject
                    {
                        ["id"] = new OpenApiInteger(2),
                        ["name"] = new OpenApiString("Noura"),
                        ["email"] = new OpenApiString("noura@example.com"),
                        ["createdAt"] = new OpenApiString("2025-08-12T05:21:07Z")
                    }
                };
            }

            OpenApiDocs.Ensure409(op, "Duplicate email", "Email is already in use.");

            op.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content =
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiObject
                        {
                            ["name"]     = new OpenApiString("Noura"),
                            ["email"]    = new OpenApiString("noura@example.com"),
                            ["password"] = new OpenApiString("P@ssw0rd!")
                        }
                    }
                }
            };
            return op;
        });

        // PUT: update user
        group.MapPut("/{id:int}", async (int id, UpdateUserDto input, AppDbContext db) =>
        {
            try
            {
                var user = await db.Users.FindAsync(id);
                if (user is null) return Results.NotFound();

                if (!string.IsNullOrWhiteSpace(input.Email))
                {
                    var normalized = input.Email.Trim().ToLowerInvariant();
                    var exists = await db.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == normalized);
                    if (exists)
                        return Results.Conflict(new ProblemDetails { Title = "Duplicate email", Detail = "Email is already in use." });

                    user.Email = input.Email.Trim();
                }

                if (!string.IsNullOrWhiteSpace(input.Name))
                    user.Name = input.Name.Trim();

                if (!string.IsNullOrWhiteSpace(input.Password))
                    user.Password = input.Password; // hash in real apps

                await db.SaveChangesAsync();
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error updating user: {ex.Message}");
                return Results.Problem("An error occurred while updating the user.", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("UpdateUser")
        .WithSummary("Update a user")
        .WithDescription("Updates name, email, and/or password. Returns 204 on success.")
        .Accepts<UpdateUserDto>("application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .AddEndpointFilter<ValidationFilter<UpdateUserDto>>()
        .WithOpenApi(op =>
        {
            op.Responses.Remove("200");
            OpenApiDocs.Ensure404(op, "User with id 42 was not found.");
            OpenApiDocs.Ensure409(op, "Duplicate email", "Email is already in use.");

            op.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content =
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiObject
                        {
                            ["name"]     = new OpenApiString("Noura"),
                            ["email"]    = new OpenApiString("noura@example.com"),
                            ["password"] = new OpenApiString("P@ssw0rd!")
                        }
                    }
                }
            };
            return op;
        });

        // DELETE: remove user
        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            try
            {
                var user = await db.Users.FindAsync(id);
                if (user is null) return Results.NotFound();

                db.Users.Remove(user);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deleting user: {ex.Message}");
                return Results.Problem("An error occurred while deleting the user.", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("DeleteUser")
        .WithSummary("Delete a user")
        .WithDescription("Deletes a user by id. Returns 204 on success.")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi(op =>
        {
            op.Responses.Remove("200");

            OpenApiDocs.Ensure404(op, "User with id 42 was not found.");
            return op;
        });

        return group;
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Name, u.Email, u.CreatedAt);
}

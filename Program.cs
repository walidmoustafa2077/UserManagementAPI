using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserManagementApi.Api.Endpoints;
using UserManagementApi.Data;
using UserManagementApi.Validators;

var builder = WebApplication.CreateBuilder(args);

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Management API",
        Version = "v1",
        Description = "Minimal API using EF Core InMemory. Passwords are never returned."
    });

    // XML comments (DTOs, etc.)
    var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

    // Add JWT bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

});

// EF Core InMemory
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("UserManagementDb"));

// JWT settings
var jwtKey = builder.Configuration["Jwt:Key"] ?? "this_is_a_super_long_secret_key_1234567890";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "yourapp";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddLogging();
builder.Services.AddAuthorization();

builder.Services.AddTransient<ErrorHandlingMiddleware>();

// FluentValidation (auto-discovery)
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();

var app = builder.Build();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// Map endpoints
app.MapUsersEndpoints();


app.MapPost("/login", (string username, string password) =>
{
    // For demo: hard-coded authentication check
    if (username != "admin" || password != "1234")
    {
        return Results.Unauthorized();
    }

    var jwtKey = builder.Configuration["Jwt:Key"] ?? "this_is_a_super_long_secret_key_1234567890";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "yourapp";

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Role, "Admin")
    };

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: null, // can set if needed
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new { token = tokenString });
})
.WithName("Login")
.WithSummary("Authenticate and get JWT token")
.WithDescription("Returns a JWT token for valid username and password")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized);


// Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!await db.Users.AnyAsync())
    {
        db.Users.Add(new UserManagementApi.Models.User
        {
            Name = "Admin",
            Email = "admin@techhive.com",
            Password = "1234"
        });
        await db.SaveChangesAsync();
    }
}

app.Run();

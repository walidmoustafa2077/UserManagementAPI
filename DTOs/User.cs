namespace UserManagementApi.DTOs;

public record UserDto(int Id, string Name, string Email, DateTime CreatedAt);

public record CreateUserDto(string Name, string Email, string Password);

public record UpdateUserDto(string? Name, string? Email, string? Password);

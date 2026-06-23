namespace ImageGallery.Api.DTOs;

public record AdminUserDto(
    Guid Id,
    string Username,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt);

public record CreateUserByAdminRequest(string Username, string Email, string Password, string Role);

namespace ImageGallery.Api.DTOs;

public record RegisterRequest(string Username, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);

public record UserDto(Guid Id, string Username, string Email, string Role, string Status);

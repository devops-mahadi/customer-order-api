namespace AuthService.DTOs;

public record LoginResponse
{
    public required string Token { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public required int ExpiresIn { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

namespace AuthService.Services;

public interface IJwtTokenService
{
    string GenerateToken(string email, string role);
    DateTime GetTokenExpiration();
}

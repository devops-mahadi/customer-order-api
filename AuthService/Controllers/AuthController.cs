using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IJwtTokenService jwtTokenService) : ControllerBase
{
    // Hardcoded users for demo - replace with database lookup in production
    private static readonly Dictionary<string, (string Password, string Role)> Users = new()
    {
        { "john.doe@example.com", ("password123", "Customer") },
        { "jane.smith@example.com", ("password123", "Customer") },
        { "bob.johnson@example.com", ("password123", "Customer") },
        { "admin@example.com", ("admin123", "Admin") }
    };

    /// <summary>
    /// Login endpoint to generate JWT token
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate credentials
        if (!Users.TryGetValue(request.Email, out var user) || user.Password != request.Password)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Generate JWT token
        var token = jwtTokenService.GenerateToken(request.Email, user.Role);
        var expiresAt = jwtTokenService.GetTokenExpiration();

        var response = new LoginResponse
        {
            Token = token,
            Email = request.Email,
            Role = user.Role,
            ExpiresIn = 3600, // 60 minutes in seconds
            ExpiresAt = expiresAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Get list of available demo users
    /// </summary>
    [HttpGet("demo-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDemoUsers()
    {
        var demoUsers = Users.Select(u => new
        {
            Email = u.Key,
            Password = u.Value.Password,
            Role = u.Value.Role
        });

        return Ok(new
        {
            message = "Demo users for testing (remove this endpoint in production)",
            users = demoUsers
        });
    }
}

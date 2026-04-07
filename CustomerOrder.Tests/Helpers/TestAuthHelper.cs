using AuthService.Services;
using Microsoft.Extensions.Configuration;

namespace CustomerOrder.Tests.Helpers;

/// <summary>
/// Helper class to generate and cache JWT tokens for integration tests
/// Uses the actual JwtTokenService from AuthService
/// </summary>
public static class TestAuthHelper
{
    private static string? _cachedToken;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets a cached JWT token for testing. Token is generated once and reused.
    /// </summary>
    public static string GetTestToken()
    {
        if (_cachedToken != null)
        {
            return _cachedToken;
        }

        lock (_lock)
        {
            if (_cachedToken != null)
            {
                return _cachedToken;
            }

            // Create test configuration matching AuthService settings
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtKey"] = "CustomerOrderApi-SecureKey-MinimumOf32Characters-ForJwtTokenGeneration",
                    ["JwtIssuer"] = "CustomerOrderApi",
                    ["JwtAudience"] = "CustomerOrderClient"
                })
                .Build();

            // Create JWT service and generate token
            var jwtService = new JwtTokenService(configuration);
            _cachedToken = jwtService.GenerateToken("test@example.com", "Customer");

            return _cachedToken;
        }
    }

    /// <summary>
    /// Resets the cached token. Useful for tests that need a fresh token.
    /// </summary>
    public static void ResetToken()
    {
        lock (_lock)
        {
            _cachedToken = null;
        }
    }
}

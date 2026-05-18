using GrammarCorrector.Models;
using System.Security.Claims;

namespace GrammarCorrector.Services;

/// <summary>
/// Interface for authentication service.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user account.
    /// </summary>
    Task<AuthResult> RegisterAsync(string email, string fullName, string password);

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    Task<AuthResult> LoginAsync(string email, string password);

    /// <summary>
    /// Generates a JWT token for a user.
    /// </summary>
    string GenerateToken(User user);

    /// <summary>
    /// Validates a token and returns the claims principal.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Refreshes a user's subscription if it has expired.
    /// </summary>
    Task<bool> RefreshSubscriptionAsync(int userId);

    /// <summary>
    /// Checks if a user's subscription is active.
    /// </summary>
    Task<bool> IsSubscriptionActiveAsync(int userId);

    /// <summary>
    /// Gets user by email.
    /// </summary>
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Gets user by ID.
    /// </summary>
    Task<User?> GetUserByIdAsync(int userId);
}

/// <summary>
/// Result of authentication operation.
/// </summary>
public class AuthResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public User? User { get; set; }
    public string? Token { get; set; }
    public DateTime? TokenExpiration { get; set; }
}

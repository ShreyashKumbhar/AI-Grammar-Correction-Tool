using GrammarCorrector.Data;
using GrammarCorrector.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GrammarCorrector.Services;

/// <summary>
/// Authentication service implementation with JWT token generation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user with email, name, and password.
    /// </summary>
    public async Task<AuthResult> RegisterAsync(string email, string fullName, string password)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(email))
                return new AuthResult { Success = false, Message = "Email is required." };

            if (string.IsNullOrWhiteSpace(fullName))
                return new AuthResult { Success = false, Message = "Full name is required." };

            if (string.IsNullOrWhiteSpace(password))
                return new AuthResult { Success = false, Message = "Password is required." };

            if (password.Length < 8)
                return new AuthResult { Success = false, Message = "Password must be at least 8 characters long." };

            // Check if user already exists
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
                return new AuthResult { Success = false, Message = "User with this email already exists." };

            // Create new user
            var user = new User
            {
                Email = email,
                FullName = fullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                SubscriptionTier = SubscriptionTier.Free,
                CreatedAt = DateTime.UtcNow,
                SubscriptionStartDate = DateTime.UtcNow,
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Initialize usage metrics for current month
            var now = DateTime.UtcNow;
            var usageMetric = new UsageMetrics
            {
                UserId = user.Id,
                Year = now.Year,
                Month = now.Month,
                CorrectionCount = 0,
                TotalCharactersProcessed = 0,
                TotalErrorsDetected = 0,
                QuotaLimit = 500, // Free tier quota
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.UsageMetrics.Add(usageMetric);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"User registered successfully: {email}");

            var token = GenerateToken(user);
            return new AuthResult
            {
                Success = true,
                Message = "User registered successfully.",
                User = user,
                Token = token,
                TokenExpiration = DateTime.UtcNow.AddMinutes(
                    _configuration.GetValue<int>("Jwt:ExpirationMinutes", 1440))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return new AuthResult
            {
                Success = false,
                Message = "An error occurred during registration. Please try again."
            };
        }
    }

    /// <summary>
    /// Authenticates user with email and password.
    /// </summary>
    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return new AuthResult { Success = false, Message = "Email is required." };

            if (string.IsNullOrWhiteSpace(password))
                return new AuthResult { Success = false, Message = "Password is required." };

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning($"Failed login attempt for email: {email}");
                return new AuthResult
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            if (!user.IsActive)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Your account has been deactivated."
                };
            }

            // Check subscription status
            if (user.SubscriptionTier == SubscriptionTier.Unlimited)
            {
                if (user.SubscriptionEndDate.HasValue && user.SubscriptionEndDate < DateTime.UtcNow)
                {
                    // Subscription expired, downgrade to free tier
                    user.SubscriptionTier = SubscriptionTier.Free;
                    user.SubscriptionEndDate = null;
                    await _db.SaveChangesAsync();
                }
            }

            var token = GenerateToken(user);
            _logger.LogInformation($"User logged in successfully: {email}");

            return new AuthResult
            {
                Success = true,
                Message = "Login successful.",
                User = user,
                Token = token,
                TokenExpiration = DateTime.UtcNow.AddMinutes(
                    _configuration.GetValue<int>("Jwt:ExpirationMinutes", 1440))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return new AuthResult
            {
                Success = false,
                Message = "An error occurred during login. Please try again."
            };
        }
    }

    /// <summary>
    /// Generates a JWT token for a user.
    /// </summary>
    public string GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("SubscriptionTier", user.SubscriptionTier.ToString()),
            new Claim("IsActive", user.IsActive.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                _configuration.GetValue<int>("Jwt:ExpirationMinutes", 1440)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validates a JWT token and returns claims principal.
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    /// <summary>
    /// Refreshes a user's subscription status.
    /// </summary>
    public async Task<bool> RefreshSubscriptionAsync(int userId)
    {
        try
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return false;

            if (user.SubscriptionTier == SubscriptionTier.Unlimited &&
                user.SubscriptionEndDate.HasValue &&
                user.SubscriptionEndDate < DateTime.UtcNow)
            {
                // Subscription expired, downgrade to free tier
                user.SubscriptionTier = SubscriptionTier.Free;
                user.SubscriptionEndDate = null;
                await _db.SaveChangesAsync();
                _logger.LogInformation($"Subscription expired for user {userId}, downgraded to Free tier");
                return false;
            }

            return user.SubscriptionTier == SubscriptionTier.Unlimited;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error refreshing subscription for user {userId}");
            return false;
        }
    }

    /// <summary>
    /// Checks if a user's subscription is currently active.
    /// </summary>
    public async Task<bool> IsSubscriptionActiveAsync(int userId)
    {
        try
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return false;

            if (user.SubscriptionTier == SubscriptionTier.Free)
                return true; // Free tier is always active

            if (user.SubscriptionEndDate.HasValue && user.SubscriptionEndDate < DateTime.UtcNow)
            {
                // Subscription expired
                return false;
            }

            return user.SubscriptionTier == SubscriptionTier.Unlimited;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking subscription status for user {userId}");
            return false;
        }
    }

    /// <summary>
    /// Gets a user by email.
    /// </summary>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user by email: {email}");
            return null;
        }
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            return await _db.Users.FindAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user by ID: {userId}");
            return null;
        }
    }
}

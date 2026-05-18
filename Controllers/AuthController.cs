using GrammarCorrector.Models;
using GrammarCorrector.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GrammarCorrector.Controllers;

/// <summary>
/// Authentication controller for user signup, login, and account management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/auth/signup
    /// Registers a new user account.
    /// </summary>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(SignupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse { Error = "Invalid request data." });

        var result = await _authService.RegisterAsync(request.Email, request.FullName, request.Password);

        if (!result.Success)
            return BadRequest(new ErrorResponse { Error = result.Message ?? "Registration failed." });

        return Ok(new SignupResponse
        {
            Message = result.Message,
            Token = result.Token,
            TokenExpiration = result.TokenExpiration,
            User = new UserResponse
            {
                Id = result.User!.Id,
                Email = result.User.Email,
                FullName = result.User.FullName,
                SubscriptionTier = result.User.SubscriptionTier.ToString()
            }
        });
    }

    /// <summary>
    /// POST /api/auth/login
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse { Error = "Invalid request data." });

        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (!result.Success)
            return Unauthorized(new ErrorResponse { Error = result.Message ?? "Login failed." });

        return Ok(new LoginResponse
        {
            Message = result.Message,
            Token = result.Token,
            TokenExpiration = result.TokenExpiration,
            User = new UserResponse
            {
                Id = result.User!.Id,
                Email = result.User.Email,
                FullName = result.User.FullName,
                SubscriptionTier = result.User.SubscriptionTier.ToString()
            }
        });
    }

    /// <summary>
    /// GET /api/auth/profile
    /// Gets the current authenticated user's profile.
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new ErrorResponse { Error = "Invalid user ID in token." });

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new ErrorResponse { Error = "User not found." });

            if (!user.IsActive)
                return Forbid();

            return Ok(new ProfileResponse
            {
                User = new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    SubscriptionTier = user.SubscriptionTier.ToString()
                },
                SubscriptionStartDate = user.SubscriptionStartDate,
                SubscriptionEndDate = user.SubscriptionEndDate,
                CreatedAt = user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "An error occurred while retrieving your profile." });
        }
    }

    /// <summary>
    /// POST /api/auth/change-password
    /// Changes the password for the authenticated user.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return BadRequest(new ErrorResponse { Error = "Current password is required." });

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new ErrorResponse { Error = "New password is required." });

            if (request.NewPassword.Length < 8)
                return BadRequest(new ErrorResponse { Error = "New password must be at least 8 characters long." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new ErrorResponse { Error = "Invalid user ID in token." });

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new ErrorResponse { Error = "User not found." });

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return BadRequest(new ErrorResponse { Error = "Current password is incorrect." });

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
            db.Users.Update(user);
            await db.SaveChangesAsync();

            _logger.LogInformation($"Password changed for user {userId}");

            return Ok(new SuccessResponse { Message = "Password changed successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "An error occurred while changing your password." });
        }
    }
}

/// <summary>
/// Request DTOs
/// </summary>
public class SignupRequest
{
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

/// <summary>
/// Response DTOs
/// </summary>
public class SignupResponse
{
    public string? Message { get; set; }
    public string? Token { get; set; }
    public DateTime? TokenExpiration { get; set; }
    public UserResponse? User { get; set; }
}

public class LoginResponse
{
    public string? Message { get; set; }
    public string? Token { get; set; }
    public DateTime? TokenExpiration { get; set; }
    public UserResponse? User { get; set; }
}

public class ProfileResponse
{
    public UserResponse? User { get; set; }
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string SubscriptionTier { get; set; } = null!;
}

public class ErrorResponse
{
    public string? Error { get; set; }
}

public class SuccessResponse
{
    public string? Message { get; set; }
}

using FluentValidation;
using LMS.Application.Auth;
using LMS.Application.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;

namespace LMS.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IAccountLifecycleService _lifecycle;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        IAuthService auth,
        IAccountLifecycleService lifecycle,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _auth = auth;
        _lifecycle = lifecycle;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(BuildModelState(validation));
        }

        var result = await _auth.RegisterAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(BuildModelState(validation));
        }

        var result = await _auth.LoginAsync(request, ct);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);
        return Ok(new { id, email, role });
    }

    // ------------------------------------------------------------------
    // Email verification + password reset
    // ------------------------------------------------------------------

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kicks off a password-reset email. Always returns 200 even when the
    /// email isn't on file — silent no-op prevents account enumeration.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            await _lifecycle.IssuePasswordResetAsync(request.Email, ct);
        }
        return Ok(new
        {
            message = "If that email is registered, a reset link has been sent.",
        });
    }

    public class ResetPasswordRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Token and new password are both required." });
        }
        if (request.Password.Length < 8
            || !request.Password.Any(char.IsUpper)
            || !request.Password.Any(char.IsLower)
            || !request.Password.Any(char.IsDigit))
        {
            return BadRequest(new
            {
                message = "Password must be at least 8 characters and include upper, lower, and digit.",
            });
        }
        var ok = await _lifecycle.ConsumePasswordResetAsync(request.Token, request.Password, ct);
        return ok
            ? Ok(new { message = "Password updated. You can sign in now." })
            : BadRequest(new { message = "That reset link is invalid or has expired." });
    }

    public class VerifyEmailRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        var ok = await _lifecycle.ConsumeEmailVerificationAsync(request.Token, ct);
        return ok
            ? Ok(new { message = "Email verified." })
            : BadRequest(new { message = "That verification link is invalid or has expired." });
    }

    /// <summary>
    /// Re-issue a verification email for the signed-in user. Useful when they
    /// missed the original or it expired. Idempotent — no harm in spamming.
    /// </summary>
    [HttpPost("resend-verification")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendVerification(CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var userId))
        {
            return Unauthorized();
        }
        await _lifecycle.IssueEmailVerificationAsync(userId, ct);
        return Ok(new { message = "Verification email sent." });
    }

    private static ModelStateDictionary BuildModelState(FluentValidation.Results.ValidationResult result)
    {
        var ms = new ModelStateDictionary();
        foreach (var error in result.Errors)
        {
            ms.AddModelError(error.PropertyName, error.ErrorMessage);
        }
        return ms;
    }
}

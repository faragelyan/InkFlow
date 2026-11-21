using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InkFlow.Application.Features.Auth.DTOs.Requests;
using InkFlow.Application.Features.Auth.DTOs.Responses;
using InkFlow.Application.Features.Auth.Interfaces;

namespace InkFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    #region 1. Registration & Login

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Registration Failed",
                Detail = result.Error,
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(result.Value);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Authentication Failed",
                Detail = result.Error,
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(result.Value);
    }

    #endregion

    #region 2. Token & Session Management

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Refresh Token Failed",
                Detail = result.Error,
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost("revoke-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RevokeTokenAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Revoke Token Failed",
                Detail = result.Error,
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(new { message = "Token successfully revoked." });
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _authService.LogoutAsync(userId, request?.RefreshToken, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Logout Failed",
                Detail = result.Error,
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(new { message = "Logged out successfully." });
    }

    #endregion

    #region 3. Email Verification & Password Management

    [HttpPost("confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ConfirmEmailAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Email Confirmation Failed",
                Detail = result.Error,
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(new { message = "Email confirmed successfully." });
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPasswordAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Forgot Password Request Failed",
                Detail = result.Error,
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(new { message = "If your email is registered, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Password Reset Failed",
                Detail = result.Error,
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(new { message = "Password reset successfully. You can now login with your new password." });
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _authService.ChangePasswordAsync(userId, request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Change Password Failed",
                Detail = result.Error,
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(new { message = "Password changed successfully." });
    }

    #endregion

    #region 4. Social / External Authentication

    [HttpPost("google-auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.GoogleAuthAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Google Authentication Failed",
                Detail = result.Error,
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(result.Value);
    }

    #endregion
}

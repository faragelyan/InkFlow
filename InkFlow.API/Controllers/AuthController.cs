using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InkFlow.Application.Features.Auth.DTOs.Requests;
using InkFlow.Application.Features.Auth.DTOs.Responses;
using InkFlow.Application.Interfaces.Services;

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
                Detail = result.Error
            });
        }

        return Ok(result.Value);
    }

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
                Detail = result.Error
            });
        }

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost("revoke-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RevokeTokenAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Revoke Token Failed",
                Detail = result.Error
            });
        }

        return Ok(new { message = "Token successfully revoked." });
    }
}

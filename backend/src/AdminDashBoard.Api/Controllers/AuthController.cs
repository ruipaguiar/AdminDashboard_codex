using System.Security.Claims;
using AdminDashBoard.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminDashBoard.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserAuthService _userAuthService;
    private readonly IAntiforgery _antiforgery;

    public AuthController(
        IUserAuthService userAuthService,
        IAntiforgery antiforgery)
    {
        _userAuthService = userAuthService;
        _antiforgery = antiforgery;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["credentials"] = ["Email and password are required."]
            }));
        }

        var valid = await _userAuthService.ValidateCredentialsAsync(
            email,
            request.Password,
            cancellationToken);

        if (!valid)
        {
            return Unauthorized();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, email)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            });

        return Ok(new AuthUserResponse(email));
    }

    [Authorize]
    [HttpGet("csrf")]
    public ActionResult<CsrfTokenResponse> Csrf()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new CsrfTokenResponse(tokens.RequestToken ?? string.Empty));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<AuthUserResponse> Me()
    {
        return Ok(new AuthUserResponse(GetCurrentEmail()));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userAuthService.ChangePasswordAsync(
            GetCurrentEmail(),
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        return result switch
        {
            ChangePasswordResult.Success => NoContent(),
            ChangePasswordResult.InvalidCurrentPassword => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["currentPassword"] = ["Current password is incorrect."]
            })),
            ChangePasswordResult.InvalidNewPassword => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["newPassword"] = ["Use a password with at least 8 characters."]
            })),
            _ => Unauthorized()
        };
    }

    private string GetCurrentEmail()
    {
        return User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record AuthUserResponse(string Email);

public sealed record CsrfTokenResponse(string Token);

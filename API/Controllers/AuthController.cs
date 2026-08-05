using API.Models.Auth;
using API.Constants;
using API.Services;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
	private readonly UserManager<ApplicationUser> userManager;
	private readonly SignInManager<ApplicationUser> signInManager;
	private readonly IJwtTokenService jwtTokenService;
	private readonly ILogger<AuthController> logger;

	public AuthController(
		UserManager<ApplicationUser> userManager,
		SignInManager<ApplicationUser> signInManager,
		IJwtTokenService jwtTokenService,
		ILogger<AuthController> logger)
	{
		this.userManager = userManager;
		this.signInManager = signInManager;
		this.jwtTokenService = jwtTokenService;
		this.logger = logger;
	}

	[HttpPost("register")]
	[ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> Register(RegisterRequest request)
	{
		if (request.Password != request.ConfirmPassword)
		{
			ModelState.AddModelError(nameof(request.ConfirmPassword), "Password confirmation does not match.");
			return ValidationProblem(ModelState);
		}

		var user = new ApplicationUser
		{
			FullName = request.FullName.Trim(),
			UserName = request.Email.Trim(),
			Email = request.Email.Trim(),
			IsActive = true
		};

		var result = await userManager.CreateAsync(user, request.Password);

		if (!result.Succeeded)
		{
			logger.LogWarning("Registration failed for {Email}. Errors: {Errors}", request.Email, string.Join(" | ", result.Errors.Select(error => error.Description)));
			foreach (var error in result.Errors)
			{
				ModelState.AddModelError(error.Code, error.Description);
			}

			return ValidationProblem(ModelState);
		}

		var addToRoleResult = await userManager.AddToRoleAsync(user, IdentityRoles.User);
		if (!addToRoleResult.Succeeded)
		{
			logger.LogError("Failed to assign default role to {Email}. Errors: {Errors}", request.Email, string.Join(" | ", addToRoleResult.Errors.Select(error => error.Description)));
			await userManager.DeleteAsync(user);
			return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Registration could not be completed. Please try again." });
		}

		logger.LogInformation("User {Email} registered successfully.", user.Email);

		return Ok(await CreateAuthResponseAsync(user));
	}

	[HttpPost("login")]
	[ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Login(LoginRequest request)
	{
		var email = request.Email.Trim();
		var user = await userManager.FindByEmailAsync(email);

		if (user is null || !user.IsActive)
		{
			logger.LogWarning("Login failed for {Email}: user missing or inactive.", email);
			return Unauthorized(new { message = "Invalid email or password." });
		}

		var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

		if (result.IsLockedOut)
		{
			logger.LogWarning("Login blocked for {Email}: account locked out.", email);
			return Unauthorized(new { message = "Account is locked. Please try again later." });
		}

		if (!result.Succeeded)
		{
			logger.LogWarning("Login failed for {Email}: invalid credentials.", email);
			return Unauthorized(new { message = "Invalid email or password." });
		}

		logger.LogInformation("User {Email} logged in successfully.", email);

		return Ok(await CreateAuthResponseAsync(user));
	}

	[Authorize]
	[HttpGet("me")]
	[ProducesResponseType<AuthUserResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Me()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (!Guid.TryParse(userId, out var parsedUserId))
		{
			return Unauthorized();
		}

		var user = await userManager.FindByIdAsync(parsedUserId.ToString());

		if (user is null)
		{
			return Unauthorized();
		}

		return Ok(new AuthUserResponse
		{
			Id = user.Id.ToString(),
			FullName = user.FullName,
			Email = user.Email ?? string.Empty,
			Roles = (await userManager.GetRolesAsync(user)).ToArray()
		});
	}

	[Authorize]
	[HttpPost("logout")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public IActionResult Logout()
	{
		var email = User.FindFirstValue(ClaimTypes.Email) ?? "unknown";
		logger.LogInformation("User {Email} logged out.", email);
		return NoContent();
	}

	private async Task<AuthResponse> CreateAuthResponseAsync(ApplicationUser user)
	{
		var roles = await userManager.GetRolesAsync(user);
		var token = await jwtTokenService.CreateTokenAsync(user, roles);

		return new AuthResponse
		{
			AccessToken = token.AccessToken,
			ExpiresAtUtc = token.ExpiresAtUtc,
			User = new AuthUserResponse
			{
				Id = user.Id.ToString(),
				FullName = user.FullName,
				Email = user.Email ?? string.Empty,
				Roles = roles.ToArray()
			}
		};
	}
}
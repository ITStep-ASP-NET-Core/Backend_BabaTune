using BabaTune.Application.DTO.Auth;
using BabaTune.Application.Interfaces;
using BabaTune.WebApi.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/auth")]
[AllowAnonymous]
public class AuthController : BaseApiController
{
	private readonly IAuthService _authService;

	public AuthController ( IAuthService authService )
	{
		_authService = authService;
	}

	[HttpPost("register")]
	public async Task<IActionResult> Register ( [FromBody] RegisterDto dto )
	{
		var result = await _authService.RegisterAsync(dto);
		return result.Success
			? StatusCode(StatusCodes.Status201Created, result.Data)
			: Failure(result.Error);
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login ( [FromBody] LoginDto dto )
	{
		var result = await _authService.LoginAsync(dto);
		return result.Success
			? Ok(result.Data)
			: Problem(detail: result.Error, statusCode: StatusCodes.Status401Unauthorized);
	}

	[HttpPost("refresh")]
	public async Task<IActionResult> Refresh ( [FromBody] RefreshRequest request )
	{
		var result = await _authService.RefreshAsync(request.RefreshToken);
		return result.Success
			? Ok(result.Data)
			: Problem(detail: result.Error, statusCode: StatusCodes.Status401Unauthorized);
	}

	[HttpPost("logout")]
	public async Task<IActionResult> Logout ( [FromBody] RefreshRequest request )
	{
		await _authService.LogoutAsync(request.RefreshToken);
		return NoContent();
	}
}
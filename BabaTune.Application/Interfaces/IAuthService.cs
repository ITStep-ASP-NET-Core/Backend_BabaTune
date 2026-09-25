using BabaTune.Application.Common;
using BabaTune.Application.DTO.Auth;

namespace BabaTune.Application.Interfaces
{
	public interface IAuthService
	{
		Task<Result<AuthResponseDto>> RegisterAsync ( RegisterDto dto );
		Task<Result<AuthResponseDto>> LoginAsync ( LoginDto dto );
		Task<Result<AuthResponseDto>> RefreshAsync ( string refreshToken );
		Task LogoutAsync ( string refreshToken );
	}
}

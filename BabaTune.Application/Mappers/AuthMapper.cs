using BabaTune.Application.DTO.Auth;
using BabaTune.Application.DTO.Users;

namespace BabaTune.Application.Mappers
{
	public class AuthMapper
	{
		public static AuthResponseDto ToDto ( string accessToken, string refreshToken, DateTime expiresAt, UserDto user ) => new()
		{
			AccessToken = accessToken,
			RefreshToken = refreshToken,
			ExpiresAt = expiresAt,
			User = user,
		};
	}
}

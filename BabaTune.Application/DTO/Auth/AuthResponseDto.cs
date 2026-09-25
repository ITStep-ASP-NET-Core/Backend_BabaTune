using BabaTune.Application.DTO.Users;

namespace BabaTune.Application.DTO.Auth
{
	public class AuthResponseDto
	{
		public string AccessToken { get; set; } = string.Empty;
		public string RefreshToken { get; set; } = string.Empty;
		public DateTime ExpiresAt { get; set; }
		public UserDto User { get; set; } = null!;
	}
}

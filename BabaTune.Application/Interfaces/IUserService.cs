using BabaTune.Application.Common;
using BabaTune.Application.DTO.Users;
using BabaTune.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.Interfaces
{
	public interface IUserService
	{
		Task<UserDto?> GetProfileAsync ( Guid userId );
		Task<UserSummaryDto?> GetShortAsync ( Guid userId );
		Task<PagedResult<UserSummaryDto>> SearchAsync ( string query, int pageNumber, int pageSize );
		Task<Result> UpdateInfoAsync ( Guid userId, UpdateUserDto dto );
		Task<Result> UpdatePasswordAsync ( Guid userId, string currentPassword, string newPassword );
		Task<Result> UpdateAvatarAsync ( Guid userId, IFormFile? avatarFile );
	}
}

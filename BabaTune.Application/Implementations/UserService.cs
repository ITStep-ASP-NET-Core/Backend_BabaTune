using BabaTune.Application.Common;
using BabaTune.Application.DTO.Users;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.Implementations
{
	public class UserService : IUserService
	{
		private readonly IUnitOfWork _uow;
		private readonly IPasswordHasher _passwordHasher;

		public UserService ( IUnitOfWork uow, IPasswordHasher passwordHasher )
		{
			_uow = uow;
			_passwordHasher = passwordHasher;
		}

		public async Task<UserDto?> GetProfileAsync ( Guid userId )
		{
			var user = await _uow.Users.GetByIdAsync(userId);
			if (user is null)
				return null;

			var subscriptions = await _uow.Subscribes.GetSubscriptionsCountAsync(userId);
			var tracks = await _uow.Songs.GetCountByAuthorAsync(userId);
			var listenersCount = await _uow.Subscribes.GetSubscribersCountAsync(userId);

			return UserMapper.ToDto(user, subscriptions, tracks, listenersCount);
		}

		public async Task<UserSummaryDto?> GetShortAsync ( Guid userId )
		{
			var user = await _uow.Users.GetByIdAsync(userId);
			if (user is null)
				return null;

			return UserMapper.ToSummaryDto(user);
		}

		public async Task<PagedResult<UserSummaryDto>> SearchAsync ( string query, int pageNumber, int pageSize )
		{
			var result = await _uow.Users.SearchByNameAsync(query, pageNumber, pageSize);

			return new PagedResult<UserSummaryDto>
			{
				Items = result.Items.Select(UserMapper.ToSummaryDto).ToList(),
				PageNumber = result.PageNumber,
				PageSize = result.PageSize,
				TotalCount = result.TotalCount
			};
		}

		public async Task<Result> UpdateInfoAsync ( Guid userId, UpdateUserDto dto )
		{
			var user = await _uow.Users.GetByIdAsync(userId);
			if (user is null)
				return Result.Fail("User not found.");

			user.Name = dto.Name;
			user.UpdatedAt = DateTime.UtcNow;

			_uow.Users.Update(user);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> UpdatePasswordAsync ( Guid userId, string currentPassword, string newPassword )
		{
			var user = await _uow.Users.GetByIdAsync(userId);
			if (user is null)
				return Result.Fail("User not found.");

			if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
				return Result.Fail("Current password is incorrect.");

			user.PasswordHash = _passwordHasher.HashPassword(newPassword);
			user.UpdatedAt = DateTime.UtcNow;

			_uow.Users.Update(user);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> UpdateAvatarAsync ( Guid userId, IFormFile? avatarFile )
		{
			var user = await _uow.Users.GetByIdAsync(userId);
			if (user is null)
				return Result.Fail("User not found.");

			if (user.AvatarUrl is not null)
				await _uow.Storage.DeleteAsync(user.AvatarUrl);

			if (avatarFile is not null)
			{
				await using var avatarStream = avatarFile.OpenReadStream();
				user.AvatarUrl = await _uow.Storage.UploadAsync(avatarStream, avatarFile.FileName, avatarFile.ContentType, "users/avatars");
			}
			else
			{
				user.AvatarUrl = null;
			}

			user.UpdatedAt = DateTime.UtcNow;

			_uow.Users.Update(user);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}
	}
}

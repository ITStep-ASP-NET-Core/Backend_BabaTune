using BabaTune.Application.Common;
using BabaTune.Application.DTO.Chats;
using BabaTune.Application.DTO.Songs;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Interfaces;

namespace BabaTune.Application.Implementations
{
	public class ChatService : IChatService
	{
		private const int MaxTextLength = 2000;

		private readonly IUnitOfWork _uow;

		public ChatService ( IUnitOfWork uow )
		{
			_uow = uow;
		}

		public async Task<Result<PagedResult<MessageDto>>> GetMessagesAsync ( Guid chatId, Guid userId, int pageNumber, int pageSize )
		{
			if (!await _uow.Chats.IsParticipantAsync(chatId, userId))
				return Result<PagedResult<MessageDto>>.Fail("Chat not found.");

			var messages = await _uow.Chats.GetMessagesWithUsersAsync(chatId, pageNumber, pageSize);

			var songIds = messages.Items.OfType<SongMessage>().Select(m => m.SongId).Distinct().ToList();
			var songsById = new Dictionary<Guid, Song>();
			var likedIds = new HashSet<Guid>();

			if (songIds.Count > 0)
			{
				songsById = (await _uow.Songs.GetByIdsAsync(songIds)).ToDictionary(s => s.Id);
				likedIds = await _uow.Playlists.GetLikedSongIdsAsync(userId, songIds);
			}

			var items = messages.Items
				.Select(m => MessageMapper.ToDto(
					m,
					UserMapper.ToSummaryDto(m.User!),
					m is SongMessage sm && songsById.TryGetValue(sm.SongId, out var song)
						? SongMapper.ToDto(song, likedIds.Contains(song.Id))
						: null))
				.ToList();

			return Result.Ok(new PagedResult<MessageDto>
			{
				Items = items,
				PageNumber = messages.PageNumber,
				PageSize = messages.PageSize,
				TotalCount = messages.TotalCount
			});
		}

		public async Task<Result<MessageDto>> SendTextAsync ( Guid chatId, Guid userId, string text )
		{
			var trimmed = text?.Trim();
			if (string.IsNullOrEmpty(trimmed))
				return Result<MessageDto>.Fail("Message text is required.");

			if (trimmed.Length > MaxTextLength)
				return Result<MessageDto>.Fail("Message text is too long.");

			if (!await _uow.Chats.IsParticipantAsync(chatId, userId))
				return Result<MessageDto>.Fail("Chat not found.");

			var user = await _uow.Users.GetByIdAsync(userId);
			if (user is null)
				return Result<MessageDto>.Fail("User not found.");

			var now = DateTime.UtcNow;

			var message = new TextMessage
			{
				Id = Guid.NewGuid(),
				ChatId = chatId,
				UserId = userId,
				Text = trimmed,
				CreatedAt = now,
				UpdatedAt = now
			};

			return await SaveAsync(message, user, null, trimmed);
		}

		public async Task<Result<MessageDto>> SendSongAsync ( Guid chatId, Guid userId, Guid songId )
		{
			if (!await _uow.Chats.IsParticipantAsync(chatId, userId))
				return Result<MessageDto>.Fail("Chat not found.");

			var song = await _uow.Songs.GetWithAllAsync(songId);
			if (song is null)
				return Result<MessageDto>.Fail("Song not found.");

			var user = await _uow.Users.GetByIdAsync(userId);
			if (user is null)
				return Result<MessageDto>.Fail("User not found.");

			var now = DateTime.UtcNow;

			var message = new SongMessage
			{
				Id = Guid.NewGuid(),
				ChatId = chatId,
				UserId = userId,
				SongId = songId,
				CreatedAt = now,
				UpdatedAt = now
			};

			var isLiked = (await _uow.Playlists.GetLikedSongIdsAsync(userId, [songId])).Contains(songId);

			return await SaveAsync(message, user, SongMapper.ToDto(song, isLiked), song.Name);
		}

		private async Task<Result<MessageDto>> SaveAsync ( Message message, User user, SongDto? song, string preview )
		{
			await _uow.Chats.AddMessageAsync(message);
			await _uow.SaveChangesAsync();

			return Result.Ok(MessageMapper.ToDto(message, UserMapper.ToSummaryDto(user), song));
		}

	}
}

using BabaTune.Application.Common;
using BabaTune.Application.DTO.Rooms;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Interfaces;

namespace BabaTune.Application.Implementations
{
	public class RoomService : IRoomService
	{

		private readonly IUnitOfWork _uow;

		public RoomService ( IUnitOfWork uow )
		{
			_uow = uow;
		}

		public async Task<RoomDto?> GetByIdAsync ( Guid roomId, Guid? currentUserId )
		{
			var room = await _uow.Rooms.GetWithDetailsAsync(roomId);
			if (room is null || !await HasAccessAsync(room, currentUserId))
				return null;

			var songs = room.Queue.Select(q => q.Song).Append(room.CurrentSong);
			var likedSongIds = await GetLikedAsync(currentUserId, songs);

			return RoomMapper.ToDto(room, likedSongIds);
		}

		public async Task<RoomCardDto?> GetByOwnerAsync ( Guid ownerId, Guid? currentUserId )
		{
			var room = await _uow.Users.GetOwnedRoomAsync(ownerId);
			if (room is null || !await HasAccessAsync(room, currentUserId))
				return null;

			var likedSongIds = await GetLikedAsync(currentUserId, [room.CurrentSong]);

			return RoomMapper.ToCardDto(room, likedSongIds);
		}

		public async Task<PagedResult<RoomCardDto>> GetPublicAsync ( int pageNumber, int pageSize, Guid? currentUserId )
		{
			var rooms = await _uow.Rooms.GetPublicRankedAsync(pageNumber, pageSize);
			return await ToCardPageAsync(rooms, currentUserId);
		}

		public async Task<PagedResult<RoomCardDto>> GetFriendsRoomsAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var rooms = await _uow.Rooms.GetByFriendsAsync(userId, pageNumber, pageSize);
			return await ToCardPageAsync(rooms, userId);
		}

		public async Task<PagedResult<RoomCardDto>> GetByCurrentSongAsync ( Guid songId, int pageNumber, int pageSize, Guid? currentUserId )
		{
			var rooms = await _uow.Rooms.GetByCurrentSongAsync(songId, RoomType.Public, pageNumber, pageSize);
			return await ToCardPageAsync(rooms, currentUserId);
		}

		public async Task<Result<RoomDto>> CreateAsync ( CreateRoomDto dto, Guid ownerId )
		{
			if (!Enum.IsDefined(dto.Type))
				return Result<RoomDto>.Fail("Invalid room type.");

			var owner = await _uow.Users.GetByIdAsync(ownerId);
			if (owner is null)
				return Result<RoomDto>.Fail("User not found.");

			if (await _uow.Users.GetOwnedRoomAsync(ownerId) is not null)
				return Result<RoomDto>.Fail("You already own a room.");

			await DetachFromCurrentRoomAsync(owner);

			var now = DateTime.UtcNow;

			var chat = new Chat
			{
				Id = Guid.NewGuid(),
				Type = ChatType.Room,
				CreatedAt = now,
				UpdatedAt = now
			};

			var room = new Room
			{
				Id = Guid.NewGuid(),
				OwnerId = ownerId,
				ChatId = chat.Id,
				Type = dto.Type,
				CreatedAt = now,
				UpdatedAt = now
			};

			await _uow.Chats.AddAsync(chat);
			await _uow.Rooms.AddAsync(room);
			await _uow.Rooms.AddMemberAsync(room.Id, ownerId);

			await _uow.SaveChangesAsync();

			var created = await GetByIdAsync(room.Id, ownerId);

			return Result.Ok(created!);
		}

		public async Task<Result> DeleteAsync ( Guid roomId, Guid userId, bool isAdmin )
		{
			var room = await _uow.Rooms.GetByIdAsync(roomId);
			if(room is null)
				return Result<Room>.Fail("Room not found.");

			if(room.OwnerId != userId && !isAdmin)
				return Result<Room>.Fail("You are not the owner of this room.");

			await DeleteRoomAsync(room);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> JoinAsync ( Guid roomId, Guid userId )
		{
			var room = await _uow.Rooms.GetByIdAsync(roomId);
			if (room is null || !await HasAccessAsync(room, userId))
				return Result.Fail("Room not found.");

			var user = await _uow.Users.GetByIdAsync(userId);
			if (user is null)
				return Result.Fail("User not found.");

			if (user.RoomId == roomId)
				return Result.Fail("Already in this room.");

			await DetachFromCurrentRoomAsync(user);
			await _uow.Rooms.AddMemberAsync(roomId, userId);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> LeaveAsync ( Guid roomId, Guid userId )
		{
			var user = await _uow.Users.GetByIdAsync(userId);
			if (user is null || user.RoomId != roomId)
				return Result.Fail("You are not a member of this room.");

			await LeaveRoomAsync(user, roomId);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> TransferOwnershipAsync ( Guid roomId, Guid userId, Guid newOwnerId )
		{
			var owned = await LoadOwnedAsync(roomId, userId);
			if (!owned.Success)
				return Result.Fail(owned.Error!);

			if (newOwnerId == userId)
				return Result.Fail("You are already the owner.");

			var newOwner = await _uow.Users.GetByIdAsync(newOwnerId);
			if (newOwner is null || newOwner.RoomId != roomId)
				return Result.Fail("User is not a member of this room.");

			var room = owned.Data!;
			room.OwnerId = newOwnerId;
			room.UpdatedAt = DateTime.UtcNow;

			_uow.Rooms.Update(room);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> AddSongAsync ( Guid roomId, Guid userId, Guid songId )
		{
			var owned = await LoadOwnedAsync(roomId, userId);
			if (!owned.Success)
				return Result.Fail(owned.Error!);

			var song = await _uow.Songs.GetByIdAsync(songId);
			if (song is null)
				return Result.Fail("Song not found.");

			await EnqueueAsync(owned.Data!, [songId]);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> AddPlaylistAsync ( Guid roomId, Guid userId, Guid playlistId )
		{
			var owned = await LoadOwnedAsync(roomId, userId);
			if (!owned.Success)
				return Result.Fail(owned.Error!);

			var playlist = await _uow.Playlists.GetByIdAsync(playlistId);
			if (playlist is null)
				return Result.Fail("Playlist not found.");

			var songIds = await _uow.Playlists.GetSongIdsAsync(playlistId);
			if (songIds.Count == 0)
				return Result.Fail("Playlist is empty.");

			await EnqueueAsync(owned.Data!, songIds);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> AddAlbumAsync ( Guid roomId, Guid userId, Guid albumId )
		{
			var owned = await LoadOwnedAsync(roomId, userId);
			if(!owned.Success)
				return Result.Fail(owned.Error!);

			var album = await _uow.Albums.GetByIdAsync(albumId);
			if(album is null)
				return Result.Fail("Album not found.");

			var songIds = await _uow.Albums.GetSongIdsAsync(albumId);
			if(songIds.Count == 0)
				return Result.Fail("Album is empty.");

			await EnqueueAsync(owned.Data!, songIds);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> UpdatePlaybackAsync ( Guid roomId, Guid userId, UpdatePlaybackDto dto )
		{
			var owned = await LoadOwnedAsync(roomId, userId);
			if (!owned.Success)
				return Result.Fail(owned.Error!);

			var room = owned.Data!;

			if (room.CurrentSongId is null)
				return Result.Fail("Nothing is playing.");

			if (dto.PositionMs < 0)
				return Result.Fail("Position must be at least 0.");

			room.IsPlaying = dto.IsPlaying;
			room.PlaybackPositionMs = dto.PositionMs;
			room.UpdatedAt = DateTime.UtcNow;

			_uow.Rooms.Update(room);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> SkipAsync ( Guid roomId, Guid userId )
		{
			var owned = await LoadOwnedAsync(roomId, userId);
			if (!owned.Success)
				return Result.Fail(owned.Error!);

			var room = owned.Data!;
			var next = await _uow.Rooms.DequeueAsync(roomId);

			room.CurrentSongId = next?.SongId;
			room.PlaybackPositionMs = 0;
			room.IsPlaying = next is not null && room.IsPlaying;
			room.UpdatedAt = DateTime.UtcNow;

			_uow.Rooms.Update(room);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		private async Task<Result<Room>> LoadOwnedAsync ( Guid roomId, Guid userId )
		{
			var room = await _uow.Rooms.GetByIdAsync(roomId);
			if (room is null)
				return Result<Room>.Fail("Room not found.");

			if (room.OwnerId != userId)
				return Result<Room>.Fail("You are not the owner of this room.");

			return Result.Ok(room);
		}

		private async Task EnqueueAsync ( Room room, HashSet<Guid> songIds )
		{
			var queued = songIds.ToList();

			if (room.CurrentSongId is null)
			{
				room.CurrentSongId = queued[0];
				room.PlaybackPositionMs = 0;
				room.IsPlaying = false;
				queued.RemoveAt(0);
			}

			if (queued.Count == 1)
				await _uow.Rooms.AddSongAsync(room.Id, queued[0]);
			else if (queued.Count > 1)
				await _uow.Rooms.AddSongsAsync(room.Id, queued);

			room.UpdatedAt = DateTime.UtcNow;
			_uow.Rooms.Update(room);
		}

		private async Task DetachFromCurrentRoomAsync ( User user )
		{
			if (user.RoomId is { } currentRoomId)
				await LeaveRoomAsync(user, currentRoomId);
		}

		private async Task LeaveRoomAsync ( User user, Guid roomId )
		{
			await _uow.Rooms.RemoveMemberAsync(roomId, user.Id);

			var room = await _uow.Rooms.GetByIdAsync(roomId);
			if (room is null)
				return;

			var nextMemberId = await _uow.Rooms.GetRandomMemberIdAsync(roomId, user.Id);
			if (nextMemberId is null)
			{
				await DeleteRoomAsync(room);
				return;
			}

			if (room.OwnerId == user.Id)
			{
				room.OwnerId = nextMemberId.Value;
				room.UpdatedAt = DateTime.UtcNow;
				_uow.Rooms.Update(room);
			}
		}

		private async Task DeleteRoomAsync ( Room room )
		{
			var chat = await _uow.Chats.GetByIdAsync(room.ChatId);

			_uow.Rooms.Delete(room);
			if (chat is not null)
				_uow.Chats.Delete(chat);
		}

		private async Task<bool> HasAccessAsync ( Room room, Guid? userId )
		{
			if (room.Type == RoomType.Public)
				return true;

			if (userId is null)
				return false;

			if (room.OwnerId == userId.Value)
				return true;

			var friendship = await _uow.Friendships.GetByUsersAsync(userId.Value, room.OwnerId);
			return friendship is { Status: FriendshipStatus.Accepted };
		}

		private async Task<HashSet<Guid>> GetLikedAsync ( Guid? userId, IEnumerable<Song?> songs )
		{
			if (userId is null)
				return [];

			var ids = songs.Where(s => s is not null).Select(s => s!.Id).Distinct().ToList();
			return await _uow.Playlists.GetLikedSongIdsAsync(userId.Value, ids);
		}

		private async Task<PagedResult<RoomCardDto>> ToCardPageAsync ( PagedResult<Room> rooms, Guid? currentUserId )
		{
			var likedSongIds = await GetLikedAsync(currentUserId, rooms.Items.Select(r => r.CurrentSong));

			return new PagedResult<RoomCardDto>
			{
				Items = rooms.Items.Select(r => RoomMapper.ToCardDto(r, likedSongIds)).ToList(),
				PageNumber = rooms.PageNumber,
				PageSize = rooms.PageSize,
				TotalCount = rooms.TotalCount
			};
		}
	}
}

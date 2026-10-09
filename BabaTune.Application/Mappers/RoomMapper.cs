using BabaTune.Application.DTO.Rooms;
using BabaTune.Application.DTO.Songs;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Mappers
{
	public static class RoomMapper
	{
		public static RoomCardDto ToCardDto ( Room room, HashSet<Guid> likedSongIds )
		{
			return new RoomCardDto
			{
				Id = room.Id,
				Owner = room.Owner is null ? null : UserMapper.ToSummaryDto(room.Owner),
				Type = room.Type,
				CurrentSong = ToSongDto(room.CurrentSong, likedSongIds),
				IsPlaying = room.IsPlaying,
				UpdatedAt = room.UpdatedAt
			};
		}

		public static RoomDto ToDto ( Room room, HashSet<Guid> likedSongIds )
		{
			return new RoomDto
			{
				Id = room.Id,
				Owner = room.Owner is null ? null : UserMapper.ToSummaryDto(room.Owner),
				Type = room.Type,
				CurrentSong = ToSongDto(room.CurrentSong, likedSongIds),
				IsPlaying = room.IsPlaying,
				UpdatedAt = room.UpdatedAt,
				ChatId = room.ChatId,
				PlaybackPositionMs = room.PlaybackPositionMs,
				Members = room.Members.Select(UserMapper.ToSummaryDto).ToList(),
				Queue = room.Queue
					.OrderBy(q => q.AddedAt)
					.Select(q => new QueueItemDto
					{
						Id = q.Id,
						Song = ToSongDto(q.Song, likedSongIds)!,
						AddedAt = q.AddedAt
					})
					.ToList()
			};
		}

		private static SongDto? ToSongDto ( Song? song, HashSet<Guid> likedSongIds )
			=> song is null ? null : SongMapper.ToDto(song, likedSongIds.Contains(song.Id));
	}
}

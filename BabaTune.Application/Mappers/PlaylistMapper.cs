using BabaTune.Application.DTO.Playlists;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Mappers
{
	public class PlaylistMapper
	{
		public static PlaylistDto ToDto ( Playlist playlist, int songsCount, int totalDuration ) => new()
		{
			Id = playlist.Id,
			Name = playlist.Name,
			Type = playlist.Type,
			UserId = playlist.UserId,
			ImageUrl = playlist.ImageUrl,
			SongsCount = songsCount,
			TotalDuration = totalDuration,
		};

		public static PlaylistSidebarDto ToSidebarDto ( Playlist playlist ) => new()
		{
			Id = playlist.Id,
			Name = playlist.Name,
			Type = playlist.Type,
			ImageUrl = playlist.ImageUrl,
		};

		public static Playlist ToEntity ( CreatePlaylistDto playlistDto, Guid userId, string? imageUrl ) => new()
		{
			Id = Guid.NewGuid(),
			Name = playlistDto.Name,
			Type = PlaylistType.Custom,
			ImageUrl = imageUrl,
			UserId = userId,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow,
		};
	}
}

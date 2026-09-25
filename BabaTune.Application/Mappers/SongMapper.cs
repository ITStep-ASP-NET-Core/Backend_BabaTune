using BabaTune.Application.DTO.Songs;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Mappers
{
	public class SongMapper
	{
		public static SongDto ToDto ( Song song, bool isLiked ) => new()
		{
			Id = song.Id,
			Name = song.Name,
			Description = song.Description,
			Duration = song.Duration,
			Url = song.Url,
			ImageUrl = song.ImageUrl,
			Album = song.Album is null ? null : AlbumMapper.ToSummaryDto(song.Album),
			Author = song.User is null ? null : UserMapper.ToSummaryDto(song.User),
			IsLiked = isLiked,
			CreatedAt = song.CreatedAt,
		};

		public static Song ToEntity ( CreateSongDto songDto, Guid userId, string Url, string? imageUrl ) => new()
		{
			Id = Guid.NewGuid(),
			Name = songDto.Name,
			Description = songDto.Description,
			AlbumId = songDto.AlbumId,
			UserId = userId,
			Url = Url,
			ImageUrl = imageUrl,
		};
	}
}

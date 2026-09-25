using BabaTune.Application.DTO.Albums;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Mappers
{
	public class AlbumMapper
	{
		public static AlbumDto ToDto ( Album album ) => new()
		{
			Id = album.Id,
			Name = album.Name,
			Description = album.Description,
			ImageUrl = album.ImageUrl,
			AuthorId = album.User?.Id,
			AuthorName = album.User?.Name,
			CreatedAt = album.CreatedAt,
		};

		public static AlbumCardDto ToCardDto ( Album album ) => new()
		{
			Id = album.Id,
			Name = album.Name,
			ImageUrl = album.ImageUrl,
			AuthorId = album.User?.Id,
			AuthorName = album.User?.Name,
		};

		public static AlbumSummaryDto ToSummaryDto ( Album album ) => new()
		{
			Id = album.Id,
			Name = album.Name,
			ImageUrl = album.ImageUrl,
		};

		public static Album ToEntity ( CreateAlbumDto albumDto, Guid userId, string? imageUrl ) => new()
		{
			Id = Guid.NewGuid(),
			Name = albumDto.Name,
			Description = albumDto.Description,
			ImageUrl = imageUrl,
			UserId = userId,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow,
		};
	}
}

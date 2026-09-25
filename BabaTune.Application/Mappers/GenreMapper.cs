using BabaTune.Application.DTO.Genre;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Mappers
{
	public class GenreMapper
	{
		public static GenreDto ToDto ( Genre genre ) => new()
		{
			Id = genre.Id,
			Name = genre.Name,
			ImageUrl = genre.ImageUrl,
		};

		public static Genre ToEntity ( CreateGenreDto dto, string? imageUrl ) => new()
		{
			Name = dto.Name,
			ImageUrl = imageUrl,
		};
	}
}

using BabaTune.Application.DTO.Category;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Mappers
{
	public class CategoryMapper
	{
		public static CategoryDto ToDto ( Category category ) => new()
		{
			Id = category.Id,
			Name = category.Name,
			ImageUrl = category.ImageUrl,
		};

		public static Category ToEntity ( CreateCategoryDto dto, string? imageUrl ) => new()
		{
			Name = dto.Name,
			ImageUrl = imageUrl,
		};
	}
}

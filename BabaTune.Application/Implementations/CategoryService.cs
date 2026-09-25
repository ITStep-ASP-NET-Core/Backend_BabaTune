using BabaTune.Application.Common;
using BabaTune.Application.DTO.Category;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.Implementations
{
	public class CategoryService : ICategoryService
	{
		private const string DefaultCategoryImageUrl = "https://storage.babatune.app/defaults/category-cover.png";

		private readonly IUnitOfWork _uow;

		public CategoryService ( IUnitOfWork uow )
		{
			_uow = uow;
		}

		public async Task<List<CategoryDto>> GetAllAsync ( )
		{
			var categories = await _uow.Categories.GetAllAsync();
			return categories.Select(CategoryMapper.ToDto).ToList();
		}

		public async Task<CategoryDto?> GetByIdAsync ( int id )
		{
			var category = await _uow.Categories.GetByIdAsync(id);
			return category is null ? null : CategoryMapper.ToDto(category);
		}

		public async Task<PagedResult<CategoryDto>> SearchAsync ( string query, int pageNumber, int pageSize )
		{
			var categories = await _uow.Categories.SearchByNameAsync(query, pageNumber, pageSize);
			return new PagedResult<CategoryDto>
			{
				Items = categories.Items.Select(CategoryMapper.ToDto).ToList(),
				PageNumber = categories.PageNumber,
				PageSize = categories.PageSize,
				TotalCount = categories.TotalCount
			};
		}

		public async Task<Result> CreateAsync ( CreateCategoryDto dto )
		{
			var imageUrl = DefaultCategoryImageUrl;
			if (dto.ImageFile is not null)
			{
				await using var imageStream = dto.ImageFile.OpenReadStream();
				imageUrl = await _uow.Storage.UploadAsync(imageStream, dto.ImageFile.FileName, dto.ImageFile.ContentType, "categories/covers");
			}

			var category = CategoryMapper.ToEntity(dto, imageUrl);

			await _uow.Categories.AddAsync(category);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> UpdateInfoAsync ( int id, UpdateCategoryDto dto )
		{
			var category = await _uow.Categories.GetByIdAsync(id);
			if (category is null)
				return Result.Fail("Category not found.");

			category.Name = dto.Name ?? category.Name;

			_uow.Categories.Update(category);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> UpdateImageAsync ( int id, IFormFile? imageFile )
		{
			var category = await _uow.Categories.GetByIdAsync(id);
			if (category is null)
				return Result.Fail("Category not found.");

			if (category.ImageUrl is not null && category.ImageUrl != DefaultCategoryImageUrl)
				await _uow.Storage.DeleteAsync(category.ImageUrl);

			if (imageFile is not null)
			{
				await using var imageStream = imageFile.OpenReadStream();
				category.ImageUrl = await _uow.Storage.UploadAsync(imageStream, imageFile.FileName, imageFile.ContentType, "categories/covers");
			}
			else
			{
				category.ImageUrl = DefaultCategoryImageUrl;
			}

			_uow.Categories.Update(category);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> DeleteAsync ( int id )
		{
			var category = await _uow.Categories.GetByIdAsync(id);
			if (category is null)
				return Result.Fail("Category not found.");

			if (category.ImageUrl is not null && category.ImageUrl != DefaultCategoryImageUrl)
				await _uow.Storage.DeleteAsync(category.ImageUrl);

			_uow.Categories.Delete(category);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}
	}
}

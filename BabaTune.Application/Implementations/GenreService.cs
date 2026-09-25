using BabaTune.Application.Common;
using BabaTune.Application.DTO.Genre;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.Implementations
{
	public class GenreService : IGenreService
	{
		private const string DefaultGenreImageUrl = "https://storage.babatune.app/defaults/genre-cover.png";

		private readonly IUnitOfWork _uow;

		public GenreService ( IUnitOfWork uow )
		{
			_uow = uow;
		}

		public async Task<List<GenreDto>> GetAllAsync ( )
		{
			var genres = await _uow.Genres.GetAllAsync();
			return genres.Select(GenreMapper.ToDto).ToList();
		}

		public async Task<GenreDto?> GetByIdAsync ( int id )
		{
			var genre = await _uow.Genres.GetByIdAsync(id);
			return genre is null ? null : GenreMapper.ToDto(genre);
		}

		public async Task<PagedResult<GenreDto>> SearchAsync ( string query, int pageNumber, int pageSize )
		{
			var genres = await _uow.Genres.SearchByNameAsync(query, pageNumber, pageSize);
			return new PagedResult<GenreDto>
			{
				Items = genres.Items.Select(GenreMapper.ToDto).ToList(),
				PageNumber = genres.PageNumber,
				PageSize = genres.PageSize,
				TotalCount = genres.TotalCount
			};
		}

		public async Task<Result> CreateAsync ( CreateGenreDto dto )
		{
			var imageUrl = DefaultGenreImageUrl;
			if (dto.ImageFile is not null)
			{
				await using var imageStream = dto.ImageFile.OpenReadStream();
				imageUrl = await _uow.Storage.UploadAsync(imageStream, dto.ImageFile.FileName, dto.ImageFile.ContentType, "genres/covers");
			}

			var genre = new Genre
			{
				Name = dto.Name,
				ImageUrl = imageUrl,
			};

			await _uow.Genres.AddAsync(genre);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> UpdateInfoAsync ( int id, UpdateGenreDto dto )
		{
			var genre = await _uow.Genres.GetByIdAsync(id);
			if (genre is null)
				return Result.Fail("Genre not found.");

			genre.Name = dto.Name ?? genre.Name;

			_uow.Genres.Update(genre);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> UpdateImageAsync ( int id, IFormFile? imageFile )
		{
			var genre = await _uow.Genres.GetByIdAsync(id);
			if (genre is null)
				return Result.Fail("Genre not found.");

			if (genre.ImageUrl is not null && genre.ImageUrl != DefaultGenreImageUrl)
				await _uow.Storage.DeleteAsync(genre.ImageUrl);

			if (imageFile is not null)
			{
				await using var imageStream = imageFile.OpenReadStream();
				genre.ImageUrl = await _uow.Storage.UploadAsync(imageStream, imageFile.FileName, imageFile.ContentType, "genres/covers");
			}
			else
			{
				genre.ImageUrl = DefaultGenreImageUrl;
			}

			_uow.Genres.Update(genre);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> DeleteAsync ( int id )
		{
			var genre = await _uow.Genres.GetByIdAsync(id);
			if (genre is null)
				return Result.Fail("Genre not found.");

			if (genre.ImageUrl is not null && genre.ImageUrl != DefaultGenreImageUrl)
				await _uow.Storage.DeleteAsync(genre.ImageUrl);

			_uow.Genres.Delete(genre);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}
	}
}


namespace BabaTune.Application.DTO.Albums
{
	public class AlbumSummaryDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? ImageUrl { get; set; }
	}
}

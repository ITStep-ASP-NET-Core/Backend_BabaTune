
namespace BabaTune.Application.DTO.Songs
{
	public class UpdateSongDto
	{
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
		public List<int> CategoryIds { get; set; } = [];
		public List<int> GenreIds { get; set; } = [];
	}
}

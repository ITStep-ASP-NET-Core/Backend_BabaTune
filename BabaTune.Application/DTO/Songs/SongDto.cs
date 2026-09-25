using BabaTune.Application.DTO.Albums;
using BabaTune.Application.DTO.Users;

namespace BabaTune.Application.DTO.Songs
{
	public class SongDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
		public int Duration { get; set; }
		public string Url { get; set; } = string.Empty;
		public string? ImageUrl { get; set; }
		public AlbumSummaryDto? Album { get; set; }
		public UserSummaryDto? Author { get; set; }
		public bool IsLiked { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}

using BabaTune.Domain.Entities;

namespace BabaTune.Application.DTO.Playlists
{
	public class PlaylistDto
	{
		public Guid Id { get; set; }
		public string? Name { get; set; }
		public PlaylistType Type { get; set; }
		public Guid UserId { get; set; }
		public string UserName { get; set; } = string.Empty;
		public string? ImageUrl { get; set; }
		public int TotalDuration { get; set; }
		public int SongsCount { get; set; }
	}
}

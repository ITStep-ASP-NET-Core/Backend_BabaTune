using BabaTune.Domain.Entities;

namespace BabaTune.Application.DTO.Playlists
{
	public class PlaylistSidebarDto
	{
		public Guid Id { get; set; }
		public string? Name { get; set; }
		public PlaylistType Type { get; set; }
		public string? ImageUrl { get; set; }
	}
}

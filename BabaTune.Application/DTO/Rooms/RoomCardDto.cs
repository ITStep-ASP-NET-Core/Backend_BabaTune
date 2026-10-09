using BabaTune.Application.DTO.Songs;
using BabaTune.Application.DTO.Users;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.DTO.Rooms
{
	public class RoomCardDto
	{
		public Guid Id { get; set; }
		public UserSummaryDto? Owner { get; set; }
		public RoomType Type { get; set; }
		public SongDto? CurrentSong { get; set; }
		public bool IsPlaying { get; set; }
		public DateTime UpdatedAt { get; set; }
	}
}

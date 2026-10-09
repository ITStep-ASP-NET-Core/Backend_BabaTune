using BabaTune.Application.DTO.Songs;
using BabaTune.Application.DTO.Users;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.DTO.Chats
{
	public class MessageDto
	{
		public Guid Id { get; set; }
		public Guid ChatId { get; set; }
		public MessageType Type { get; set; }
		public UserSummaryDto User { get; set; } = null!;
		public string? Text { get; set; }
		public SongDto? Song { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}

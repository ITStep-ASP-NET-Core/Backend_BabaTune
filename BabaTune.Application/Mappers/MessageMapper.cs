using BabaTune.Application.DTO.Chats;
using BabaTune.Application.DTO.Songs;
using BabaTune.Application.DTO.Users;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Mappers
{
	public static class MessageMapper
	{
		public static MessageDto ToDto ( Message message, UserSummaryDto user, SongDto? song )
		{
			return new MessageDto
			{
				Id = message.Id,
				ChatId = message.ChatId,
				Type = message is SongMessage ? MessageType.Song : MessageType.Text,
				User = user,
				Text = (message as TextMessage)?.Text,
				Song = song,
				CreatedAt = message.CreatedAt
			};
		}
	}
}

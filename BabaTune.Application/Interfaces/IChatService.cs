using BabaTune.Application.Common;
using BabaTune.Application.DTO.Chats;
using BabaTune.Domain.Common;

namespace BabaTune.Application.Interfaces
{
	public interface IChatService
	{
		Task<Result<PagedResult<MessageDto>>> GetMessagesAsync ( Guid chatId, Guid userId, int pageNumber, int pageSize );
		Task<Result<MessageDto>> SendTextAsync ( Guid chatId, Guid userId, string text );
		Task<Result<MessageDto>> SendSongAsync ( Guid chatId, Guid userId, Guid songId );
	}
}

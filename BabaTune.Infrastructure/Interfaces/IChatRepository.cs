using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface IChatRepository : IGenericRepository<Chat, Guid>
	{
		Task<PagedResult<Message>> GetMessagesAsync ( Guid chatId, int pageNumber, int pageSize );
		Task<PagedResult<Message>> GetMessagesWithUsersAsync ( Guid chatId, int pageNumber, int pageSize );
		Task AddMessageAsync ( Message message );
		Task<bool> IsParticipantAsync ( Guid chatId, Guid userId );
		Task<Guid?> GetPeerIdAsync ( Guid chatId, Guid userId );
	}
}

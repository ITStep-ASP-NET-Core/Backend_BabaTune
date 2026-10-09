using BabaTune.Application.Common;
using BabaTune.Application.DTO.Rooms;
using BabaTune.Domain.Common;

namespace BabaTune.Application.Interfaces
{
	public interface IRoomService
	{
		Task<RoomDto?> GetByIdAsync ( Guid roomId, Guid? currentUserId );
		Task<RoomCardDto?> GetByOwnerAsync ( Guid ownerId, Guid? currentUserId );
		Task<PagedResult<RoomCardDto>> GetPublicAsync ( int pageNumber, int pageSize, Guid? currentUserId );
		Task<PagedResult<RoomCardDto>> GetFriendsRoomsAsync ( Guid userId, int pageNumber, int pageSize );
		Task<PagedResult<RoomCardDto>> GetByCurrentSongAsync ( Guid songId, int pageNumber, int pageSize, Guid? currentUserId );
		Task<Result<RoomDto>> CreateAsync ( CreateRoomDto dto, Guid ownerId );
		Task<Result> DeleteAsync ( Guid roomId, Guid userId, bool isAdmin );
		Task<Result> JoinAsync ( Guid roomId, Guid userId );
		Task<Result> LeaveAsync ( Guid roomId, Guid userId );
		Task<Result> TransferOwnershipAsync ( Guid roomId, Guid userId, Guid newOwnerId );
		Task<Result> AddSongAsync ( Guid roomId, Guid userId, Guid songId );
		Task<Result> AddPlaylistAsync ( Guid roomId, Guid userId, Guid playlistId );
		Task<Result> AddAlbumAsync ( Guid roomId, Guid userId, Guid albumId );
		Task<Result> UpdatePlaybackAsync ( Guid roomId, Guid userId, UpdatePlaybackDto dto );
		Task<Result> SkipAsync ( Guid roomId, Guid userId );
	}
}

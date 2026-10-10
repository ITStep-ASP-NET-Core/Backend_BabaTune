using BabaTune.Application.DTO.Notices;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Mappers
{
	public class NoticeMapper
	{
		public static NoticeDto ToDto ( Notice notice, int count, string? chatTitle )
		{
			var dto = new NoticeDto
			{
				Id = notice.Id,
				IsRead = notice.IsRead,
				Count = count,
				CreatedAt = notice.CreatedAt
			};

			switch(notice)
			{
				case FriendRequestNotice n:
				dto.Type = NoticeType.FriendRequest;
				dto.Actor = n.Sender is null ? null : UserMapper.ToSummaryDto(n.Sender);
				dto.TargetId = n.FriendShipId;
				break;

				case MessageNotice n:
				dto.Type = NoticeType.Message;
				dto.TargetId = n.ChatId;
				dto.Title = chatTitle;
				dto.Text = count == 1 ? n.Text : null;
				break;

				case NewSongNotice n:
				dto.Type = NoticeType.NewSong;
				dto.Actor = n.Author is null ? null : UserMapper.ToSummaryDto(n.Author);
				dto.TargetId = n.SongId;
				dto.Title = n.Song?.Name;
				dto.ImageUrl = n.Song?.ImageUrl;
				break;

				case NewAlbumNotice n:
				dto.Type = NoticeType.NewAlbum;
				dto.Actor = n.Author is null ? null : UserMapper.ToSummaryDto(n.Author);
				dto.TargetId = n.AlbumId;
				dto.Title = n.Album?.Name;
				dto.ImageUrl = n.Album?.ImageUrl;
				break;

				case NewRoomNotice n:
				dto.Type = NoticeType.NewRoom;
				dto.Actor = n.Owner is null ? null : UserMapper.ToSummaryDto(n.Owner);
				dto.TargetId = n.RoomId;
				dto.Title = n.Owner?.Name;
				break;

				case NewSubscriptionNotice n:
				dto.Type = NoticeType.NewSubscription;
				dto.Actor = n.Subscribe?.Subscriber is null ? null : UserMapper.ToSummaryDto(n.Subscribe.Subscriber);
				dto.TargetId = n.Subscribe?.SubscriberId;
				break;

				case ServerNotice n:
				dto.Type = NoticeType.Server;
				dto.Text = n.Text;
				dto.ImageUrl = n.ImageUrl;
				dto.Url = n.Url;
				break;
			}

			return dto;
		}
	}
}
using BabaTune.Application.DTO.Notices;
using BabaTune.Application.Implementations;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Moq;

namespace BabaTune.Tests.Services;

public class NoticeServiceTests
{
	private readonly UowMock _uow = new();
	private readonly NoticeService _sut;
	private readonly Guid _userId = Guid.NewGuid();

	public NoticeServiceTests ( )
	{
		_sut = new NoticeService(_uow.Object);
	}

	private static PagedResult<T> Page<T> ( params T[] items ) => new()
	{
		Items = items.ToList(),
		PageNumber = 1,
		PageSize = 20,
		TotalCount = items.Length
	};

	private List<Notice> CaptureAdded ( )
	{
		var added = new List<Notice>();
		_uow.Notices
			.Setup(n => n.AddRangeAsync(It.IsAny<IEnumerable<Notice>>()))
			.Callback<IEnumerable<Notice>>(items => added.AddRange(items))
			.Returns(Task.CompletedTask);
		return added;
	}

	[Fact]
	public async Task MarkAsRead_Missing_Fails ( )
	{
		var result = await _sut.MarkAsReadAsync(Guid.NewGuid(), _userId);

		Assert.False(result.Success);
		Assert.Equal("Notice not found.", result.Error);
	}

	[Fact]
	public async Task MarkAsRead_ForeignNotice_Fails ( )
	{
		var notice = new ServerNotice { Id = Guid.NewGuid(), RecipientId = Guid.NewGuid() };
		_uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);

		var result = await _sut.MarkAsReadAsync(notice.Id, _userId);

		Assert.False(result.Success);
		Assert.Equal("This notice does not belong to you.", result.Error);
		_uow.Notices.Verify(n => n.MarkAsReadAsync(It.IsAny<Guid>()), Times.Never);
		_uow.Notices.Verify(n => n.MarkChatAsReadAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
	}

	[Fact]
	public async Task MarkAsRead_OwnRegularNotice_MarksSingleAndSaves ( )
	{
		var notice = new ServerNotice { Id = Guid.NewGuid(), RecipientId = _userId };
		_uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);

		var result = await _sut.MarkAsReadAsync(notice.Id, _userId);

		Assert.True(result.Success);
		_uow.Notices.Verify(n => n.MarkAsReadAsync(notice.Id), Times.Once);
		_uow.Notices.Verify(n => n.MarkChatAsReadAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task MarkAsRead_OwnMessageNotice_MarksWholeChat ( )
	{
		var chatId = Guid.NewGuid();
		var notice = new MessageNotice { Id = Guid.NewGuid(), RecipientId = _userId, ChatId = chatId };
		_uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);

		var result = await _sut.MarkAsReadAsync(notice.Id, _userId);

		Assert.True(result.Success);
		_uow.Notices.Verify(n => n.MarkChatAsReadAsync(_userId, chatId), Times.Once);
		_uow.Notices.Verify(n => n.MarkAsReadAsync(It.IsAny<Guid>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task MarkChatAsRead_MarksAndSaves ( )
	{
		var chatId = Guid.NewGuid();

		await _sut.MarkChatAsReadAsync(chatId, _userId);

		_uow.Notices.Verify(n => n.MarkChatAsReadAsync(_userId, chatId), Times.Once);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task GetUnreadCount_DelegatesToRepository ( )
	{
		_uow.Notices.Setup(n => n.GetUnreadCountAsync(_userId)).ReturnsAsync(4);

		var result = await _sut.GetUnreadCountAsync(_userId);

		Assert.Equal(4, result);
	}

	[Fact]
	public async Task GetAll_RequestsFeedWithoutUnreadFilter ( )
	{
		_uow.Notices
			.Setup(n => n.GetFeedAsync(_userId, false, 2, 5))
			.ReturnsAsync(new PagedResult<NoticeFeedRow> { Items = new List<NoticeFeedRow>(), PageNumber = 2, PageSize = 5, TotalCount = 0 });

		var result = await _sut.GetAllAsync(_userId, 2, 5);

		Assert.Empty(result.Items);
		Assert.Equal(2, result.PageNumber);
		Assert.Equal(5, result.PageSize);
		_uow.Notices.Verify(n => n.GetFeedAsync(_userId, false, 2, 5), Times.Once);
	}

	[Fact]
	public async Task GetUnread_RequestsFeedWithUnreadFilter ( )
	{
		_uow.Notices
			.Setup(n => n.GetFeedAsync(_userId, true, 1, 20))
			.ReturnsAsync(new PagedResult<NoticeFeedRow> { Items = new List<NoticeFeedRow>(), PageNumber = 1, PageSize = 20, TotalCount = 0 });

		await _sut.GetUnreadAsync(_userId, 1, 20);

		_uow.Notices.Verify(n => n.GetFeedAsync(_userId, true, 1, 20), Times.Once);
	}

	[Fact]
	public async Task GetAll_EmptyFeed_SkipsDetailAndTitleLookups ( )
	{
		_uow.Notices
			.Setup(n => n.GetFeedAsync(_userId, false, 1, 20))
			.ReturnsAsync(new PagedResult<NoticeFeedRow> { Items = new List<NoticeFeedRow>(), PageNumber = 1, PageSize = 20, TotalCount = 0 });

		var result = await _sut.GetAllAsync(_userId, 1, 20);

		Assert.Empty(result.Items);
		Assert.Equal(0, result.TotalCount);
		_uow.Notices.Verify(n => n.GetDetailedByIdsAsync(It.IsAny<ICollection<Guid>>()), Times.Never);
		_uow.Notices.Verify(n => n.GetChatTitlesAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
	}

	[Fact]
	public async Task GetAll_KeepsFeedOrderSkipsMissingDetailsAndKeepsPaging ( )
	{
		var first = new ServerNotice { Id = Guid.NewGuid(), RecipientId = _userId, Text = "first" };
		var second = new ServerNotice { Id = Guid.NewGuid(), RecipientId = _userId, Text = "second" };
		var missingId = Guid.NewGuid();

		_uow.Notices
			.Setup(n => n.GetFeedAsync(_userId, false, 3, 10))
			.ReturnsAsync(new PagedResult<NoticeFeedRow>
			{
				Items = new List<NoticeFeedRow>
				{
					new() { Id = second.Id, Count = 1 },
					new() { Id = missingId, Count = 1 },
					new() { Id = first.Id, Count = 1 }
				},
				PageNumber = 3,
				PageSize = 10,
				TotalCount = 23
			});
		_uow.Notices
			.Setup(n => n.GetDetailedByIdsAsync(It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new List<Notice> { first, second });

		var result = await _sut.GetAllAsync(_userId, 3, 10);

		Assert.Equal(new[] { second.Id, first.Id }, result.Items.Select(i => i.Id).ToArray());
		Assert.Equal(3, result.PageNumber);
		Assert.Equal(10, result.PageSize);
		Assert.Equal(23, result.TotalCount);
	}

	[Fact]
	public async Task GetAll_NoMessageNotices_DoesNotLoadChatTitles ( )
	{
		var notice = new ServerNotice { Id = Guid.NewGuid(), RecipientId = _userId, Text = "hi", Url = "https://url", ImageUrl = "https://img" };
		_uow.Notices
			.Setup(n => n.GetFeedAsync(_userId, false, 1, 20))
			.ReturnsAsync(Page(new NoticeFeedRow { Id = notice.Id, Count = 1 }));
		_uow.Notices
			.Setup(n => n.GetDetailedByIdsAsync(It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new List<Notice> { notice });

		var result = await _sut.GetAllAsync(_userId, 1, 20);

		var dto = Assert.Single(result.Items);
		Assert.Equal(NoticeType.Server, dto.Type);
		Assert.Equal("hi", dto.Text);
		Assert.Equal("https://url", dto.Url);
		Assert.Equal("https://img", dto.ImageUrl);
		_uow.Notices.Verify(n => n.GetChatTitlesAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
	}

	[Fact]
	public async Task GetAll_SingleUnreadMessage_ShowsChatTitleAndText ( )
	{
		var chatId = Guid.NewGuid();
		var notice = new MessageNotice { Id = Guid.NewGuid(), RecipientId = _userId, ChatId = chatId, Text = "hello" };
		_uow.Notices
			.Setup(n => n.GetFeedAsync(_userId, false, 1, 20))
			.ReturnsAsync(Page(new NoticeFeedRow { Id = notice.Id, Count = 1 }));
		_uow.Notices
			.Setup(n => n.GetDetailedByIdsAsync(It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new List<Notice> { notice });
		_uow.Notices
			.Setup(n => n.GetChatTitlesAsync(_userId, It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new Dictionary<Guid, string> { [chatId] = "Alice" });

		var result = await _sut.GetAllAsync(_userId, 1, 20);

		var dto = Assert.Single(result.Items);
		Assert.Equal(NoticeType.Message, dto.Type);
		Assert.Equal(1, dto.Count);
		Assert.Equal("Alice", dto.Title);
		Assert.Equal("hello", dto.Text);
		Assert.Equal(chatId, dto.TargetId);
	}

	[Fact]
	public async Task GetAll_GroupedMessages_ShowsCountAndHidesText ( )
	{
		var chatId = Guid.NewGuid();
		var notice = new MessageNotice { Id = Guid.NewGuid(), RecipientId = _userId, ChatId = chatId, Text = "last" };
		_uow.Notices
			.Setup(n => n.GetFeedAsync(_userId, false, 1, 20))
			.ReturnsAsync(Page(new NoticeFeedRow { Id = notice.Id, Count = 5 }));
		_uow.Notices
			.Setup(n => n.GetDetailedByIdsAsync(It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new List<Notice> { notice });
		_uow.Notices
			.Setup(n => n.GetChatTitlesAsync(_userId, It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new Dictionary<Guid, string> { [chatId] = "Room" });

		var result = await _sut.GetAllAsync(_userId, 1, 20);

		var dto = Assert.Single(result.Items);
		Assert.Equal(5, dto.Count);
		Assert.Equal("Room", dto.Title);
		Assert.Null(dto.Text);
	}

	[Fact]
	public async Task GetAll_MessageWithUnknownChat_HasNullTitle ( )
	{
		var notice = new MessageNotice { Id = Guid.NewGuid(), RecipientId = _userId, ChatId = Guid.NewGuid(), Text = "x" };
		_uow.Notices
			.Setup(n => n.GetFeedAsync(_userId, false, 1, 20))
			.ReturnsAsync(Page(new NoticeFeedRow { Id = notice.Id, Count = 1 }));
		_uow.Notices
			.Setup(n => n.GetDetailedByIdsAsync(It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new List<Notice> { notice });
		_uow.Notices
			.Setup(n => n.GetChatTitlesAsync(_userId, It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new Dictionary<Guid, string>());

		var result = await _sut.GetAllAsync(_userId, 1, 20);

		Assert.Null(Assert.Single(result.Items).Title);
	}

	[Fact]
	public async Task GetAll_MapsActorAndTargetsForEntityNotices ( )
	{
		var author = TestData.NewUser(name: "Author");
		var song = TestData.NewSong(userId: author.Id);
		var album = new Album { Id = Guid.NewGuid(), Name = "Album", ImageUrl = "https://cloud/album.png" };
		var room = new Room { Id = Guid.NewGuid() };

		var friendRequest = new FriendRequestNotice { Id = Guid.NewGuid(), RecipientId = _userId, SenderId = author.Id, Sender = author, FriendShipId = Guid.NewGuid() };
		var newSong = new NewSongNotice { Id = Guid.NewGuid(), RecipientId = _userId, AuthorId = author.Id, Author = author, SongId = song.Id, Song = song };
		var newAlbum = new NewAlbumNotice { Id = Guid.NewGuid(), RecipientId = _userId, AuthorId = author.Id, Author = author, AlbumId = album.Id, Album = album };
		var newRoom = new NewRoomNotice { Id = Guid.NewGuid(), RecipientId = _userId, OwnerId = author.Id, Owner = author, RoomId = room.Id, Room = room };
		var subscribe = new Subscribe { Id = Guid.NewGuid(), SubscriberId = author.Id, Subscriber = author };
		var newSubscription = new NewSubscriptionNotice { Id = Guid.NewGuid(), RecipientId = _userId, SubscribeId = subscribe.Id, Subscribe = subscribe };

		var all = new Notice[] { friendRequest, newSong, newAlbum, newRoom, newSubscription };
		_uow.Notices
			.Setup(n => n.GetFeedAsync(_userId, false, 1, 20))
			.ReturnsAsync(new PagedResult<NoticeFeedRow>
			{
				Items = all.Select(n => new NoticeFeedRow { Id = n.Id, Count = 1 }).ToList(),
				PageNumber = 1,
				PageSize = 20,
				TotalCount = all.Length
			});
		_uow.Notices
			.Setup(n => n.GetDetailedByIdsAsync(It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(all.ToList());

		var result = await _sut.GetAllAsync(_userId, 1, 20);
		var items = result.Items.ToList();

		Assert.All(items, i => Assert.Equal(author.Id, i.Actor!.Id));

		Assert.Equal(NoticeType.FriendRequest, items[0].Type);
		Assert.Equal(friendRequest.FriendShipId, items[0].TargetId);

		Assert.Equal(NoticeType.NewSong, items[1].Type);
		Assert.Equal(song.Id, items[1].TargetId);
		Assert.Equal(song.Name, items[1].Title);
		Assert.Equal(song.ImageUrl, items[1].ImageUrl);

		Assert.Equal(NoticeType.NewAlbum, items[2].Type);
		Assert.Equal(album.Id, items[2].TargetId);
		Assert.Equal("Album", items[2].Title);
		Assert.Equal("https://cloud/album.png", items[2].ImageUrl);

		Assert.Equal(NoticeType.NewRoom, items[3].Type);
		Assert.Equal(room.Id, items[3].TargetId);
		Assert.Equal(author.Name, items[3].Title);

		Assert.Equal(NoticeType.NewSubscription, items[4].Type);
		Assert.Equal(author.Id, items[4].TargetId);
	}

	[Fact]
	public async Task NotifyFriendRequest_CreatesUnreadNoticeForRecipient ( )
	{
		var added = CaptureAdded();
		var senderId = Guid.NewGuid();
		var friendshipId = Guid.NewGuid();

		await _sut.NotifyFriendRequestAsync(senderId, _userId, friendshipId);

		var notice = Assert.IsType<FriendRequestNotice>(Assert.Single(added));
		Assert.Equal(senderId, notice.SenderId);
		Assert.Equal(friendshipId, notice.FriendShipId);
		Assert.Equal(_userId, notice.RecipientId);
		Assert.False(notice.IsRead);
		Assert.NotEqual(Guid.Empty, notice.Id);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task NotifyMessage_CreatesOneNoticePerDistinctRecipient ( )
	{
		var added = CaptureAdded();
		var chatId = Guid.NewGuid();
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();

		await _sut.NotifyMessageAsync(chatId, [first, second, first]);

		Assert.Equal(2, added.Count);
		Assert.All(added, n =>
		{
			var message = Assert.IsType<MessageNotice>(n);
			Assert.Equal(chatId, message.ChatId);
			Assert.Equal("hello", message.Text);
			Assert.False(message.IsRead);
		});
		Assert.Equal(new[] { first, second }.OrderBy(x => x), added.Select(n => n.RecipientId).OrderBy(x => x));
		Assert.Equal(2, added.Select(n => n.Id).Distinct().Count());
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task NotifyMessage_NoRecipients_DoesNothing ( )
	{
		await _sut.NotifyMessageAsync(Guid.NewGuid(), []);

		_uow.Notices.Verify(n => n.AddRangeAsync(It.IsAny<IEnumerable<Notice>>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
	}

	[Fact]
	public async Task NotifyNewSong_NotifiesEverySubscriber ( )
	{
		var added = CaptureAdded();
		var authorId = Guid.NewGuid();
		var songId = Guid.NewGuid();
		var subscribers = new HashSet<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
		_uow.Subscribes.Setup(s => s.GetSubscriberIdsAsync(authorId)).ReturnsAsync(subscribers);

		await _sut.NotifyNewSongAsync(authorId, songId);

		Assert.Equal(3, added.Count);
		Assert.All(added, n =>
		{
			var notice = Assert.IsType<NewSongNotice>(n);
			Assert.Equal(authorId, notice.AuthorId);
			Assert.Equal(songId, notice.SongId);
		});
		Assert.Equal(subscribers.OrderBy(x => x), added.Select(n => n.RecipientId).OrderBy(x => x));
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task NotifyNewSong_NoSubscribers_DoesNotSave ( )
	{
		var authorId = Guid.NewGuid();
		_uow.Subscribes.Setup(s => s.GetSubscriberIdsAsync(authorId)).ReturnsAsync([]);

		await _sut.NotifyNewSongAsync(authorId, Guid.NewGuid());

		_uow.Notices.Verify(n => n.AddRangeAsync(It.IsAny<IEnumerable<Notice>>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
	}

	[Fact]
	public async Task NotifyNewAlbum_NotifiesEverySubscriber ( )
	{
		var added = CaptureAdded();
		var authorId = Guid.NewGuid();
		var albumId = Guid.NewGuid();
		var subscriberId = Guid.NewGuid();
		_uow.Subscribes.Setup(s => s.GetSubscriberIdsAsync(authorId)).ReturnsAsync([subscriberId]);

		await _sut.NotifyNewAlbumAsync(authorId, albumId);

		var notice = Assert.IsType<NewAlbumNotice>(Assert.Single(added));
		Assert.Equal(authorId, notice.AuthorId);
		Assert.Equal(albumId, notice.AlbumId);
		Assert.Equal(subscriberId, notice.RecipientId);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task NotifyNewRoom_NotifiesEverySubscriber ( )
	{
		var added = CaptureAdded();
		var ownerId = Guid.NewGuid();
		var roomId = Guid.NewGuid();
		var subscriberId = Guid.NewGuid();
		_uow.Subscribes.Setup(s => s.GetSubscriberIdsAsync(ownerId)).ReturnsAsync([subscriberId]);

		await _sut.NotifyNewRoomAsync(ownerId, roomId);

		var notice = Assert.IsType<NewRoomNotice>(Assert.Single(added));
		Assert.Equal(ownerId, notice.OwnerId);
		Assert.Equal(roomId, notice.RoomId);
		Assert.Equal(subscriberId, notice.RecipientId);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task NotifyNewSubscription_CreatesNoticeForSubscribedUser ( )
	{
		var added = CaptureAdded();
		var subscribeId = Guid.NewGuid();

		await _sut.NotifyNewSubscriptionAsync(_userId, subscribeId);

		var notice = Assert.IsType<NewSubscriptionNotice>(Assert.Single(added));
		Assert.Equal(subscribeId, notice.SubscribeId);
		Assert.Equal(_userId, notice.RecipientId);
		Assert.False(notice.IsRead);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task CreateServer_UnknownRecipient_Fails ( )
	{
		var added = CaptureAdded();

		var result = await _sut.CreateServerAsync(new CreateServerNoticeDto { Text = "t", RecipientId = Guid.NewGuid() });

		Assert.False(result.Success);
		Assert.Equal("User not found.", result.Error);
		Assert.Empty(added);
		_uow.Storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
	}

	[Fact]
	public async Task CreateServer_SpecificRecipient_CreatesSingleNoticeWithoutLoadingAllUsers ( )
	{
		var added = CaptureAdded();
		var user = TestData.NewUser();
		_uow.Users.Setup(u => u.GetByIdAsync(user.Id)).ReturnsAsync(user);

		var result = await _sut.CreateServerAsync(new CreateServerNoticeDto { Text = "hello", Url = "https://url", RecipientId = user.Id });

		Assert.True(result.Success);
		var notice = Assert.IsType<ServerNotice>(Assert.Single(added));
		Assert.Equal(user.Id, notice.RecipientId);
		Assert.Equal("hello", notice.Text);
		Assert.Equal("https://url", notice.Url);
		Assert.Null(notice.ImageUrl);
		Assert.False(notice.IsRead);
		_uow.Users.Verify(u => u.GetAllIdsAsync(), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task CreateServer_Broadcast_CreatesNoticeForEveryUser ( )
	{
		var added = CaptureAdded();
		var ids = new HashSet<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
		_uow.Users.Setup(u => u.GetAllIdsAsync()).ReturnsAsync(ids);

		var result = await _sut.CreateServerAsync(new CreateServerNoticeDto { Text = "all" });

		Assert.True(result.Success);
		Assert.Equal(3, added.Count);
		Assert.All(added, n => Assert.IsType<ServerNotice>(n));
		Assert.Equal(ids.OrderBy(x => x), added.Select(n => n.RecipientId).OrderBy(x => x));
		Assert.Equal(3, added.Select(n => n.Id).Distinct().Count());
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task CreateServer_WithImage_UploadsOnceAndSharesUrlAcrossRecipients ( )
	{
		var added = CaptureAdded();
		_uow.Users.Setup(u => u.GetAllIdsAsync()).ReturnsAsync([Guid.NewGuid(), Guid.NewGuid()]);
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "n.png", "image/png", "notices/images"))
			.ReturnsAsync("https://cloud/n.png");

		var result = await _sut.CreateServerAsync(new CreateServerNoticeDto
		{
			Text = "img",
			ImageFile = FormFiles.Create("n.png", "image/png")
		});

		Assert.True(result.Success);
		Assert.All(added, n => Assert.Equal("https://cloud/n.png", ((ServerNotice)n).ImageUrl));
		_uow.Storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
	}

	[Fact]
	public async Task CreateServer_WhenSaveFails_RemovesUploadedImageAndRethrows ( )
	{
		CaptureAdded();
		_uow.Users.Setup(u => u.GetAllIdsAsync()).ReturnsAsync([Guid.NewGuid()]);
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "n.png", "image/png", "notices/images"))
			.ReturnsAsync("https://cloud/n.png");
		_uow.Mock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());

		await Assert.ThrowsAsync<InvalidOperationException>(( ) => _sut.CreateServerAsync(new CreateServerNoticeDto
		{
			Text = "img",
			ImageFile = FormFiles.Create("n.png", "image/png")
		}));

		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/n.png"), Times.Once);
	}

	[Fact]
	public async Task CreateServer_WhenCleanupFails_StillThrowsOriginalException ( )
	{
		CaptureAdded();
		_uow.Users.Setup(u => u.GetAllIdsAsync()).ReturnsAsync([Guid.NewGuid()]);
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
			.ReturnsAsync("https://cloud/n.png");
		_uow.Storage.Setup(s => s.DeleteAsync(It.IsAny<string>())).ThrowsAsync(new IOException());
		_uow.Mock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());

		await Assert.ThrowsAsync<InvalidOperationException>(( ) => _sut.CreateServerAsync(new CreateServerNoticeDto
		{
			Text = "img",
			ImageFile = FormFiles.Create("n.png", "image/png")
		}));
	}

	[Fact]
	public async Task UpdateServer_Missing_Fails ( )
	{
		var result = await _sut.UpdateServerAsync(Guid.NewGuid(), new UpdateServerNoticeDto { Text = "t" });

		Assert.False(result.Success);
		Assert.Equal("Server notice not found.", result.Error);
		_uow.Notices.Verify(n => n.Update(It.IsAny<Notice>()), Times.Never);
	}

	[Fact]
	public async Task UpdateServer_NonServerNotice_Fails ( )
	{
		var notice = new MessageNotice { Id = Guid.NewGuid(), RecipientId = _userId };
		_uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);

		var result = await _sut.UpdateServerAsync(notice.Id, new UpdateServerNoticeDto { Text = "t" });

		Assert.False(result.Success);
		Assert.Equal("Server notice not found.", result.Error);
		_uow.Notices.Verify(n => n.Update(It.IsAny<Notice>()), Times.Never);
	}

	[Fact]
	public async Task UpdateServer_Existing_ChangesTextAndUrlAndSaves ( )
	{
		var previous = DateTime.UtcNow.AddDays(-1);
		var notice = new ServerNotice { Id = Guid.NewGuid(), RecipientId = _userId, Text = "old", Url = "https://old", UpdatedAt = previous };
		_uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);

		var result = await _sut.UpdateServerAsync(notice.Id, new UpdateServerNoticeDto { Text = "new", Url = null });

		Assert.True(result.Success);
		Assert.Equal("new", notice.Text);
		Assert.Null(notice.Url);
		Assert.True(notice.UpdatedAt > previous);
		_uow.Notices.Verify(n => n.Update(notice), Times.Once);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task DeleteServer_Missing_Fails ( )
	{
		var result = await _sut.DeleteServerAsync(Guid.NewGuid());

		Assert.False(result.Success);
		Assert.Equal("Server notice not found.", result.Error);
		_uow.Notices.Verify(n => n.Delete(It.IsAny<Notice>()), Times.Never);
	}

	[Fact]
	public async Task DeleteServer_NonServerNotice_Fails ( )
	{
		var notice = new NewSongNotice { Id = Guid.NewGuid(), RecipientId = _userId };
		_uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);

		var result = await _sut.DeleteServerAsync(notice.Id);

		Assert.False(result.Success);
		_uow.Notices.Verify(n => n.Delete(It.IsAny<Notice>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
	}

	[Fact]
	public async Task DeleteServer_WithoutImage_DeletesOnlyNotice ( )
	{
		var notice = new ServerNotice { Id = Guid.NewGuid(), RecipientId = _userId };
		_uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);

		var result = await _sut.DeleteServerAsync(notice.Id);

		Assert.True(result.Success);
		_uow.Notices.Verify(n => n.Delete(notice), Times.Once);
		_uow.Notices.Verify(n => n.IsImageUsedAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
		_uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task DeleteServer_ImageNotUsedElsewhere_RemovesImageFromStorage ( )
	{
		var notice = new ServerNotice { Id = Guid.NewGuid(), RecipientId = _userId, ImageUrl = "https://cloud/n.png" };
		_uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);
		_uow.Notices.Setup(n => n.IsImageUsedAsync("https://cloud/n.png", notice.Id)).ReturnsAsync(false);

		var result = await _sut.DeleteServerAsync(notice.Id);

		Assert.True(result.Success);
		_uow.Notices.Verify(n => n.Delete(notice), Times.Once);
		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/n.png"), Times.Once);
	}

	[Fact]
	public async Task DeleteServer_ImageStillUsedByOtherNotices_KeepsImageInStorage ( )
	{
		var notice = new ServerNotice { Id = Guid.NewGuid(), RecipientId = _userId, ImageUrl = "https://cloud/n.png" };
		_uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);
		_uow.Notices.Setup(n => n.IsImageUsedAsync("https://cloud/n.png", notice.Id)).ReturnsAsync(true);

		var result = await _sut.DeleteServerAsync(notice.Id);

		Assert.True(result.Success);
		_uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task DeleteServer_WhenSaveFails_KeepsImageInStorage ( )
	{
		var notice = new ServerNotice { Id = Guid.NewGuid(), RecipientId = _userId, ImageUrl = "https://cloud/n.png" };
		_uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);
		_uow.Mock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());

		await Assert.ThrowsAsync<InvalidOperationException>(( ) => _sut.DeleteServerAsync(notice.Id));

		_uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task DeleteServer_WhenStorageCleanupFails_StillSucceeds ( )
	{
		var notice = new ServerNotice { Id = Guid.NewGuid(), RecipientId = _userId, ImageUrl = "https://cloud/n.png" };
		_uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);
		_uow.Notices.Setup(n => n.IsImageUsedAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(false);
		_uow.Storage.Setup(s => s.DeleteAsync(It.IsAny<string>())).ThrowsAsync(new IOException());

		var result = await _sut.DeleteServerAsync(notice.Id);

		Assert.True(result.Success);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}
}
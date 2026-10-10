using BabaTune.Application.Common;
using BabaTune.Application.Implementations;
using BabaTune.Application.Interfaces;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Moq;

namespace BabaTune.Tests.Services;

public class ChatServiceTests
{
	private readonly UowMock _uow = new();
	private readonly ChatService _sut;
	private readonly Mock<INoticeService> _notices = new();
	private readonly Guid _userId = Guid.NewGuid();
	private readonly Guid _chatId = Guid.NewGuid();
	private readonly Guid _peerId = Guid.NewGuid();

	public ChatServiceTests ( )
	{
		_sut = new ChatService(_uow.Object, _notices.Object);
	}

	private void Participant ( )
	{
		_uow.Chats.Setup(c => c.IsParticipantAsync(_chatId, _userId)).ReturnsAsync(true);
		_uow.Chats.Setup(c => c.GetParticipantIdsAsync(_chatId)).ReturnsAsync(new HashSet<Guid> { _userId, _peerId });
	}

	private User AddUser ( )
	{
		var user = TestData.NewUser(_userId, "Me");
		_uow.Users.Setup(u => u.GetByIdAsync(_userId)).ReturnsAsync(user);
		return user;
	}

	[Fact]
	public async Task GetMessages_NotParticipant_Fails ( )
	{
		var result = await _sut.GetMessagesAsync(_chatId, _userId, 1, 50);

		Assert.False(result.Success);
		Assert.Equal("Chat not found.", result.Error);
		_uow.Chats.Verify(c => c.GetMessagesWithUsersAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
	}

	[Fact]
	public async Task GetMessages_OnlyText_SkipsSongAndLikeLookups ( )
	{
		Participant();
		var author = TestData.NewUser(name: "Author");
		var message = new TextMessage { Id = Guid.NewGuid(), ChatId = _chatId, UserId = author.Id, User = author, Text = "hello" };
		_uow.Chats.Setup(c => c.GetMessagesWithUsersAsync(_chatId, 2, 10)).ReturnsAsync(new PagedResult<Message>
		{
			Items = new List<Message> { message },
			PageNumber = 2,
			PageSize = 10,
			TotalCount = 11
		});

		var result = await _sut.GetMessagesAsync(_chatId, _userId, 2, 10);

		Assert.True(result.Success);
		var dto = Assert.Single(result.Data!.Items);
		Assert.Equal(message.Id, dto.Id);
		Assert.Equal(MessageType.Text, dto.Type);
		Assert.Equal("hello", dto.Text);
		Assert.Null(dto.Song);
		Assert.Equal(2, result.Data.PageNumber);
		Assert.Equal(10, result.Data.PageSize);
		Assert.Equal(11, result.Data.TotalCount);
		_uow.Songs.Verify(s => s.GetByIdsAsync(It.IsAny<ICollection<Guid>>()), Times.Never);
		_uow.Playlists.Verify(p => p.GetLikedSongIdsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
	}

	[Fact]
	public async Task GetMessages_SongMessages_AttachSongsWithViewerLikes ( )
	{
		Participant();
		var author = TestData.NewUser(name: "Author");
		var liked = TestData.NewSong();
		var other = TestData.NewSong();
		ICollection<Song> songs = new List<Song> { liked, other };
		var likedMessage = new SongMessage { Id = Guid.NewGuid(), ChatId = _chatId, UserId = author.Id, User = author, SongId = liked.Id };
		var otherMessage = new SongMessage { Id = Guid.NewGuid(), ChatId = _chatId, UserId = author.Id, User = author, SongId = other.Id };
		_uow.Chats.Setup(c => c.GetMessagesWithUsersAsync(_chatId, 1, 50)).ReturnsAsync(new PagedResult<Message>
		{
			Items = new List<Message> { likedMessage, otherMessage },
			PageNumber = 1,
			PageSize = 50,
			TotalCount = 2
		});
		_uow.Songs.Setup(s => s.GetByIdsAsync(It.IsAny<ICollection<Guid>>())).ReturnsAsync(songs);
		_uow.Playlists
			.Setup(p => p.GetLikedSongIdsAsync(_userId, It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new HashSet<Guid> { liked.Id });

		var result = await _sut.GetMessagesAsync(_chatId, _userId, 1, 50);

		Assert.True(result.Success);
		var items = result.Data!.Items.ToList();
		Assert.All(items, i => Assert.Equal(MessageType.Song, i.Type));
		Assert.True(items.Single(i => i.Id == likedMessage.Id).Song!.IsLiked);
		Assert.False(items.Single(i => i.Id == otherMessage.Id).Song!.IsLiked);
		Assert.Equal(liked.Id, items.Single(i => i.Id == likedMessage.Id).Song!.Id);
	}

	[Fact]
	public async Task GetMessages_SongNoLongerExists_LeavesSongEmpty ( )
	{
		Participant();
		var author = TestData.NewUser(name: "Author");
		ICollection<Song> songs = new List<Song>();
		var message = new SongMessage { Id = Guid.NewGuid(), ChatId = _chatId, UserId = author.Id, User = author, SongId = Guid.NewGuid() };
		_uow.Chats.Setup(c => c.GetMessagesWithUsersAsync(_chatId, 1, 50)).ReturnsAsync(new PagedResult<Message>
		{
			Items = new List<Message> { message },
			PageNumber = 1,
			PageSize = 50,
			TotalCount = 1
		});
		_uow.Songs.Setup(s => s.GetByIdsAsync(It.IsAny<ICollection<Guid>>())).ReturnsAsync(songs);
		_uow.Playlists
			.Setup(p => p.GetLikedSongIdsAsync(_userId, It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new HashSet<Guid>());

		var result = await _sut.GetMessagesAsync(_chatId, _userId, 1, 50);

		Assert.True(result.Success);
		Assert.Null(Assert.Single(result.Data!.Items).Song);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task SendText_BlankText_Fails ( string? text )
	{
		var result = await _sut.SendTextAsync(_chatId, _userId, text!);

		Assert.False(result.Success);
		Assert.Equal("Message text is required.", result.Error);
		_uow.Chats.Verify(c => c.AddMessageAsync(It.IsAny<Message>()), Times.Never);
	}

	[Fact]
	public async Task SendText_TooLong_Fails ( )
	{
		Participant();
		AddUser();

		var result = await _sut.SendTextAsync(_chatId, _userId, new string('a', Constants.Limits.MaxTextLength + 1));

		Assert.False(result.Success);
		Assert.Equal("Message text is too long.", result.Error);
		_uow.Chats.Verify(c => c.AddMessageAsync(It.IsAny<Message>()), Times.Never);
	}

	[Fact]
	public async Task SendText_MaxLength_IsAccepted ( )
	{
		Participant();
		AddUser();

		var result = await _sut.SendTextAsync(_chatId, _userId, new string('a', Constants.Limits.MaxTextLength));

		Assert.True(result.Success);
	}

	[Fact]
	public async Task SendText_NotifyFails_StillSucceeds ( )
	{
		Participant();
		AddUser();
		_notices
			.Setup(n => n.NotifyMessageAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>()))
			.ThrowsAsync(new InvalidOperationException());

		var result = await _sut.SendTextAsync(_chatId, _userId, "hi");

		Assert.True(result.Success);
	}

	[Fact]
	public async Task SendText_NotParticipant_Fails ( )
	{
		AddUser();

		var result = await _sut.SendTextAsync(_chatId, _userId, "hi");

		Assert.False(result.Success);
		Assert.Equal("Chat not found.", result.Error);
		_uow.Chats.Verify(c => c.AddMessageAsync(It.IsAny<Message>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
	}

	[Fact]
	public async Task SendText_UserMissing_Fails ( )
	{
		Participant();

		var result = await _sut.SendTextAsync(_chatId, _userId, "hi");

		Assert.False(result.Success);
		Assert.Equal("User not found.", result.Error);
		_uow.Chats.Verify(c => c.AddMessageAsync(It.IsAny<Message>()), Times.Never);
	}

	[Fact]
	public async Task SendText_Valid_TrimsTextPersistsMessageAndReturnsDto ( )
	{
		Participant();
		AddUser();
		Message? added = null;
		_uow.Chats.Setup(c => c.AddMessageAsync(It.IsAny<Message>())).Callback<Message>(m => added = m).Returns(Task.CompletedTask);

		var result = await _sut.SendTextAsync(_chatId, _userId, "  hello  ");

		Assert.True(result.Success);
		var text = Assert.IsType<TextMessage>(added);
		Assert.Equal("hello", text.Text);
		Assert.Equal(_chatId, text.ChatId);
		Assert.Equal(_userId, text.UserId);
		Assert.Equal(MessageType.Text, result.Data!.Type);
		Assert.Equal("hello", result.Data.Text);
		Assert.Equal(text.Id, result.Data.Id);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
		_notices.Verify(n => n.NotifyMessageAsync(_chatId, It.Is<IEnumerable<Guid>>(r => r.Single() == _peerId)), Times.Once);
	}

	[Fact]
	public async Task SendSong_NotParticipant_Fails ( )
	{
		var result = await _sut.SendSongAsync(_chatId, _userId, Guid.NewGuid());

		Assert.False(result.Success);
		Assert.Equal("Chat not found.", result.Error);
		_uow.Chats.Verify(c => c.AddMessageAsync(It.IsAny<Message>()), Times.Never);
	}

	[Fact]
	public async Task SendSong_SongMissing_Fails ( )
	{
		Participant();
		AddUser();

		var result = await _sut.SendSongAsync(_chatId, _userId, Guid.NewGuid());

		Assert.False(result.Success);
		Assert.Equal("Song not found.", result.Error);
		_uow.Chats.Verify(c => c.AddMessageAsync(It.IsAny<Message>()), Times.Never);
	}

	[Fact]
	public async Task SendSong_UserMissing_Fails ( )
	{
		Participant();
		var song = TestData.NewSong();
		_uow.Songs.Setup(s => s.GetWithAllAsync(song.Id)).ReturnsAsync(song);

		var result = await _sut.SendSongAsync(_chatId, _userId, song.Id);

		Assert.False(result.Success);
		Assert.Equal("User not found.", result.Error);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SendSong_Valid_PersistsSongMessageWithLikeState ( bool isLiked )
	{
		Participant();
		AddUser();
		var song = TestData.NewSong();
		Message? added = null;
		_uow.Songs.Setup(s => s.GetWithAllAsync(song.Id)).ReturnsAsync(song);
		_uow.Playlists
			.Setup(p => p.GetLikedSongIdsAsync(_userId, It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(isLiked ? new HashSet<Guid> { song.Id } : new HashSet<Guid>());
		_uow.Chats.Setup(c => c.AddMessageAsync(It.IsAny<Message>())).Callback<Message>(m => added = m).Returns(Task.CompletedTask);

		var result = await _sut.SendSongAsync(_chatId, _userId, song.Id);

		Assert.True(result.Success);
		var songMessage = Assert.IsType<SongMessage>(added);
		Assert.Equal(song.Id, songMessage.SongId);
		Assert.Equal(_chatId, songMessage.ChatId);
		Assert.Equal(_userId, songMessage.UserId);
		Assert.Equal(MessageType.Song, result.Data!.Type);
		Assert.Equal(isLiked, result.Data.Song!.IsLiked);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
		_notices.Verify(n => n.NotifyMessageAsync(_chatId, It.Is<IEnumerable<Guid>>(r => r.Single() == _peerId)), Times.Once);
	}
}
using BabaTune.Application.Implementations;
using BabaTune.Application.Interfaces;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Moq;

namespace BabaTune.Tests.Services;

public class FriendshipServiceTests
{
    private readonly UowMock _uow = new();
    private readonly FriendshipService _sut;
	private readonly Mock<INoticeService> _notices = new();
	private readonly Guid _userId = Guid.NewGuid();

    public FriendshipServiceTests()
    {
        _sut = new FriendshipService(_uow.Object, _notices.Object);
    }

    private User AddUser()
    {
        var user = TestData.NewUser();
        _uow.Users.Setup(u => u.GetByIdAsync(user.Id)).ReturnsAsync(user);
        return user;
    }

    private Friendship Add(Guid senderId, Guid recipientId, FriendshipStatus status, Guid? chatId = null)
    {
        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            RecipientId = recipientId,
            Status = status,
            ChatId = chatId
        };
        _uow.Friendships.Setup(f => f.GetByIdAsync(friendship.Id)).ReturnsAsync(friendship);
        _uow.Friendships.Setup(f => f.GetByUsersAsync(senderId, recipientId)).ReturnsAsync(friendship);
        _uow.Friendships.Setup(f => f.GetByUsersAsync(recipientId, senderId)).ReturnsAsync(friendship);
        return friendship;
    }

    private static PagedResult<Friendship> Page(int pageNumber, int pageSize, int total, params Friendship[] items) => new()
    {
        Items = items.ToList(),
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalCount = total
    };

    [Fact]
    public async Task GetFriends_ReturnsOtherSideOfEachFriendshipAndKeepsPaging()
    {
        var first = TestData.NewUser(name: "First");
        var second = TestData.NewUser(name: "Second");
        var me = TestData.NewUser(_userId, "Me");
        var sent = new Friendship { Id = Guid.NewGuid(), SenderId = _userId, Sender = me, RecipientId = first.Id, Recipient = first, Status = FriendshipStatus.Accepted };
        var received = new Friendship { Id = Guid.NewGuid(), SenderId = second.Id, Sender = second, RecipientId = _userId, Recipient = me, Status = FriendshipStatus.Accepted };
        _uow.Friendships
            .Setup(f => f.GetByStatusWithUsersAsync(_userId, FriendshipStatus.Accepted, 2, 5))
            .ReturnsAsync(Page(2, 5, 11, sent, received));

        var result = await _sut.GetFriendsAsync(_userId, 2, 5);

        Assert.Equal(new[] { first.Id, second.Id }, result.Items.Select(i => i.User.Id).ToArray());
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(11, result.TotalCount);
    }

    [Fact]
    public async Task GetIncoming_ReturnsSenders()
    {
        var sender = TestData.NewUser(name: "Sender");
        var me = TestData.NewUser(_userId, "Me");
        var request = new Friendship { Id = Guid.NewGuid(), SenderId = sender.Id, Sender = sender, RecipientId = _userId, Recipient = me, Status = FriendshipStatus.Pending };
        _uow.Friendships.Setup(f => f.GetIncomingWithUsersAsync(_userId, 1, 20)).ReturnsAsync(Page(1, 20, 1, request));

        var result = await _sut.GetIncomingAsync(_userId, 1, 20);

        Assert.Equal(sender.Id, Assert.Single(result.Items).User.Id);
    }

    [Fact]
    public async Task GetBlocked_ReturnsBlockedUsers()
    {
        var blocked = TestData.NewUser(name: "Blocked");
        var me = TestData.NewUser(_userId, "Me");
        var entry = new Friendship { Id = Guid.NewGuid(), SenderId = _userId, Sender = me, RecipientId = blocked.Id, Recipient = blocked, Status = FriendshipStatus.Blocked };
        _uow.Friendships.Setup(f => f.GetBlockedWithUsersAsync(_userId, 1, 20)).ReturnsAsync(Page(1, 20, 1, entry));

        var result = await _sut.GetBlockedAsync(_userId, 1, 20);

        Assert.Equal(blocked.Id, Assert.Single(result.Items).User.Id);
    }

    [Fact]
    public async Task SendRequest_ToSelf_Fails()
    {
        var result = await _sut.SendRequestAsync(_userId, _userId);

        Assert.False(result.Success);
        Assert.Equal("You cannot send a friend request to yourself.", result.Error);
        _uow.Friendships.Verify(f => f.AddAsync(It.IsAny<Friendship>()), Times.Never);
    }

    [Fact]
    public async Task SendRequest_RecipientMissing_Fails()
    {
        var result = await _sut.SendRequestAsync(_userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("User not found.", result.Error);
        _uow.Friendships.Verify(f => f.AddAsync(It.IsAny<Friendship>()), Times.Never);
    }

    [Theory]
    [InlineData(FriendshipStatus.Accepted, true, "Already friends.")]
    [InlineData(FriendshipStatus.Accepted, false, "Already friends.")]
    [InlineData(FriendshipStatus.Pending, true, "Friend request already exists.")]
    [InlineData(FriendshipStatus.Pending, false, "Friend request already exists.")]
    [InlineData(FriendshipStatus.Blocked, true, "Unblock this user first.")]
    [InlineData(FriendshipStatus.Blocked, false, "User not found.")]
    public async Task SendRequest_ExistingRelation_FailsWithMatchingError(FriendshipStatus status, bool initiatedByMe, string expected)
    {
        var target = AddUser();
        if (initiatedByMe)
            Add(_userId, target.Id, status);
        else
            Add(target.Id, _userId, status);

        var result = await _sut.SendRequestAsync(_userId, target.Id);

        Assert.False(result.Success);
        Assert.Equal(expected, result.Error);
        _uow.Friendships.Verify(f => f.AddAsync(It.IsAny<Friendship>()), Times.Never);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SendRequest_NewRelation_PersistsPendingFriendship()
    {
        var target = AddUser();
        Friendship? added = null;
        _uow.Friendships.Setup(f => f.AddAsync(It.IsAny<Friendship>())).Callback<Friendship>(f => added = f).Returns(Task.CompletedTask);

        var result = await _sut.SendRequestAsync(_userId, target.Id);

        Assert.True(result.Success);
        Assert.NotNull(added);
        Assert.Equal(_userId, added.SenderId);
        Assert.Equal(target.Id, added.RecipientId);
        Assert.Equal(FriendshipStatus.Pending, added.Status);
        Assert.Null(added.ChatId);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Accept_Missing_Fails()
    {
        var result = await _sut.AcceptAsync(Guid.NewGuid(), _userId);

        Assert.False(result.Success);
        Assert.Equal("Friend request not found.", result.Error);
    }

    [Fact]
    public async Task Accept_BySender_Fails()
    {
        var friendship = Add(_userId, Guid.NewGuid(), FriendshipStatus.Pending);

        var result = await _sut.AcceptAsync(friendship.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("Friend request not found.", result.Error);
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
        _uow.Chats.Verify(c => c.AddAsync(It.IsAny<Chat>()), Times.Never);
    }

    [Theory]
    [InlineData(FriendshipStatus.Accepted)]
    [InlineData(FriendshipStatus.Blocked)]
    public async Task Accept_NotPending_Fails(FriendshipStatus status)
    {
        var friendship = Add(Guid.NewGuid(), _userId, status);

        var result = await _sut.AcceptAsync(friendship.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("Friend request not found.", result.Error);
        _uow.Chats.Verify(c => c.AddAsync(It.IsAny<Chat>()), Times.Never);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Accept_Pending_CreatesPersonalChatAndAccepts()
    {
        var friendship = Add(Guid.NewGuid(), _userId, FriendshipStatus.Pending);
        Chat? chat = null;
        _uow.Chats.Setup(c => c.AddAsync(It.IsAny<Chat>())).Callback<Chat>(c => chat = c).Returns(Task.CompletedTask);

        var result = await _sut.AcceptAsync(friendship.Id, _userId);

        Assert.True(result.Success);
        Assert.NotNull(chat);
        Assert.Equal(ChatType.Personal, chat.Type);
        Assert.Equal(chat.Id, friendship.ChatId);
        Assert.Equal(FriendshipStatus.Accepted, friendship.Status);
        _uow.Friendships.Verify(f => f.Update(friendship), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Block_Self_Fails()
    {
        var result = await _sut.BlockAsync(_userId, _userId);

        Assert.False(result.Success);
        Assert.Equal("You cannot block yourself.", result.Error);
    }

    [Fact]
    public async Task Block_TargetMissing_Fails()
    {
        var result = await _sut.BlockAsync(_userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("User not found.", result.Error);
    }

    [Fact]
    public async Task Block_NoRelation_CreatesBlockedEntryFromCurrentUser()
    {
        var target = AddUser();
        Friendship? added = null;
        _uow.Friendships.Setup(f => f.AddAsync(It.IsAny<Friendship>())).Callback<Friendship>(f => added = f).Returns(Task.CompletedTask);

        var result = await _sut.BlockAsync(_userId, target.Id);

        Assert.True(result.Success);
        Assert.NotNull(added);
        Assert.Equal(_userId, added.SenderId);
        Assert.Equal(target.Id, added.RecipientId);
        Assert.Equal(FriendshipStatus.Blocked, added.Status);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Block_ExistingFriendship_DeletesChatAndTakesOverBlockerRole()
    {
        var target = AddUser();
        var chat = new Chat { Id = Guid.NewGuid() };
        var friendship = Add(target.Id, _userId, FriendshipStatus.Accepted, chat.Id);
        _uow.Chats.Setup(c => c.GetByIdAsync(chat.Id)).ReturnsAsync(chat);

        var result = await _sut.BlockAsync(_userId, target.Id);

        Assert.True(result.Success);
        Assert.Equal(FriendshipStatus.Blocked, friendship.Status);
        Assert.Equal(_userId, friendship.SenderId);
        Assert.Equal(target.Id, friendship.RecipientId);
        Assert.Null(friendship.ChatId);
        _uow.Chats.Verify(c => c.Delete(chat), Times.Once);
        _uow.Friendships.Verify(f => f.Update(friendship), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Block_PendingRequest_BecomesBlockedWithoutChatLookup()
    {
        var target = AddUser();
        var friendship = Add(target.Id, _userId, FriendshipStatus.Pending);

        var result = await _sut.BlockAsync(_userId, target.Id);

        Assert.True(result.Success);
        Assert.Equal(FriendshipStatus.Blocked, friendship.Status);
        Assert.Equal(_userId, friendship.SenderId);
        _uow.Chats.Verify(c => c.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Block_AlreadyBlockedByMe_Fails()
    {
        var target = AddUser();
        Add(_userId, target.Id, FriendshipStatus.Blocked);

        var result = await _sut.BlockAsync(_userId, target.Id);

        Assert.False(result.Success);
        Assert.Equal("Already blocked.", result.Error);
        _uow.Friendships.Verify(f => f.Update(It.IsAny<Friendship>()), Times.Never);
    }

    [Fact]
    public async Task Block_BlockedByTarget_HidesExistence()
    {
        var target = AddUser();
        var friendship = Add(target.Id, _userId, FriendshipStatus.Blocked);

        var result = await _sut.BlockAsync(_userId, target.Id);

        Assert.False(result.Success);
        Assert.Equal("User not found.", result.Error);
        Assert.Equal(target.Id, friendship.SenderId);
        _uow.Friendships.Verify(f => f.Update(It.IsAny<Friendship>()), Times.Never);
    }

    [Fact]
    public async Task Remove_Missing_Fails()
    {
        var result = await _sut.RemoveAsync(Guid.NewGuid(), _userId);

        Assert.False(result.Success);
        Assert.Equal("Friendship not found.", result.Error);
    }

    [Fact]
    public async Task Remove_UnrelatedUser_Fails()
    {
        var friendship = Add(Guid.NewGuid(), Guid.NewGuid(), FriendshipStatus.Accepted);

        var result = await _sut.RemoveAsync(friendship.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("Friendship not found.", result.Error);
        _uow.Friendships.Verify(f => f.Delete(It.IsAny<Friendship>()), Times.Never);
    }

    [Fact]
    public async Task Remove_BlockedRecipient_CannotUndoBlock()
    {
        var friendship = Add(Guid.NewGuid(), _userId, FriendshipStatus.Blocked);

        var result = await _sut.RemoveAsync(friendship.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("Friendship not found.", result.Error);
        _uow.Friendships.Verify(f => f.Delete(It.IsAny<Friendship>()), Times.Never);
    }

    [Fact]
    public async Task Remove_BlockerUnblocks_DeletesEntryWithoutChat()
    {
        var friendship = Add(_userId, Guid.NewGuid(), FriendshipStatus.Blocked);

        var result = await _sut.RemoveAsync(friendship.Id, _userId);

        Assert.True(result.Success);
        _uow.Friendships.Verify(f => f.Delete(friendship), Times.Once);
        _uow.Chats.Verify(c => c.Delete(It.IsAny<Chat>()), Times.Never);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Remove_AcceptedFriendship_DeletesFriendshipAndChatForEitherSide(bool iAmSender)
    {
        var otherId = Guid.NewGuid();
        var chat = new Chat { Id = Guid.NewGuid() };
        var friendship = iAmSender
            ? Add(_userId, otherId, FriendshipStatus.Accepted, chat.Id)
            : Add(otherId, _userId, FriendshipStatus.Accepted, chat.Id);
        _uow.Chats.Setup(c => c.GetByIdAsync(chat.Id)).ReturnsAsync(chat);

        var result = await _sut.RemoveAsync(friendship.Id, _userId);

        Assert.True(result.Success);
        _uow.Chats.Verify(c => c.Delete(chat), Times.Once);
        _uow.Friendships.Verify(f => f.Delete(friendship), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Remove_PendingRequestByRecipient_Declines()
    {
        var friendship = Add(Guid.NewGuid(), _userId, FriendshipStatus.Pending);

        var result = await _sut.RemoveAsync(friendship.Id, _userId);

        Assert.True(result.Success);
        _uow.Friendships.Verify(f => f.Delete(friendship), Times.Once);
    }
}

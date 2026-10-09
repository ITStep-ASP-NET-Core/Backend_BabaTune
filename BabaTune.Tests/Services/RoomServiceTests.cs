using BabaTune.Application.DTO.Rooms;
using BabaTune.Application.Implementations;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Moq;

namespace BabaTune.Tests.Services;

public class RoomServiceTests
{
    private readonly UowMock _uow = new();
    private readonly RoomService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public RoomServiceTests()
    {
        _sut = new RoomService(_uow.Object);
        _uow.Playlists
            .Setup(p => p.GetLikedSongIdsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()))
            .ReturnsAsync(new HashSet<Guid>());
    }

    private Room Add(Guid? ownerId = null, RoomType type = RoomType.Public, Guid? currentSongId = null)
    {
        var room = new Room
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId ?? _userId,
            ChatId = Guid.NewGuid(),
            Type = type,
            CurrentSongId = currentSongId
        };
        _uow.Rooms.Setup(r => r.GetByIdAsync(room.Id)).ReturnsAsync(room);
        _uow.Rooms.Setup(r => r.GetWithDetailsAsync(room.Id)).ReturnsAsync(room);
        return room;
    }

    private User AddUser(Guid? id = null, Guid? roomId = null)
    {
        var user = TestData.NewUser(id ?? _userId);
        user.RoomId = roomId;
        _uow.Users.Setup(u => u.GetByIdAsync(user.Id)).ReturnsAsync(user);
        return user;
    }

    private void Befriend(Guid viewerId, Guid ownerId, FriendshipStatus status = FriendshipStatus.Accepted)
    {
        _uow.Friendships
            .Setup(f => f.GetByUsersAsync(viewerId, ownerId))
            .ReturnsAsync(new Friendship { Id = Guid.NewGuid(), SenderId = viewerId, RecipientId = ownerId, Status = status });
    }

    private static PagedResult<Room> Page(params Room[] items) => new()
    {
        Items = items.ToList(),
        PageNumber = 1,
        PageSize = 20,
        TotalCount = items.Length
    };

    private static HashSet<Guid> NewIds(int count) => Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToHashSet();

    [Fact]
    public async Task GetById_Missing_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), _userId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_PublicRoomAnonymous_ReturnsRoomWithoutLikeLookup()
    {
        var room = Add(ownerId: Guid.NewGuid());

        var result = await _sut.GetByIdAsync(room.Id, null);

        Assert.NotNull(result);
        Assert.Equal(room.Id, result.Id);
        Assert.Equal(room.ChatId, result.ChatId);
        _uow.Playlists.Verify(p => p.GetLikedSongIdsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task GetById_PrivateRoomAnonymous_ReturnsNull()
    {
        var room = Add(ownerId: Guid.NewGuid(), type: RoomType.Private);

        var result = await _sut.GetByIdAsync(room.Id, null);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_PrivateRoomStranger_ReturnsNull()
    {
        var room = Add(ownerId: Guid.NewGuid(), type: RoomType.Private);

        var result = await _sut.GetByIdAsync(room.Id, _userId);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(FriendshipStatus.Pending)]
    [InlineData(FriendshipStatus.Blocked)]
    public async Task GetById_PrivateRoomNotAcceptedFriend_ReturnsNull(FriendshipStatus status)
    {
        var room = Add(ownerId: Guid.NewGuid(), type: RoomType.Private);
        Befriend(_userId, room.OwnerId, status);

        var result = await _sut.GetByIdAsync(room.Id, _userId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_PrivateRoomFriend_ReturnsRoom()
    {
        var room = Add(ownerId: Guid.NewGuid(), type: RoomType.Private);
        Befriend(_userId, room.OwnerId);

        var result = await _sut.GetByIdAsync(room.Id, _userId);

        Assert.NotNull(result);
        Assert.Equal(room.Id, result.Id);
    }

    [Fact]
    public async Task GetById_PrivateRoomOwner_ReturnsRoomWithoutFriendshipLookup()
    {
        var room = Add(type: RoomType.Private);

        var result = await _sut.GetByIdAsync(room.Id, _userId);

        Assert.NotNull(result);
        _uow.Friendships.Verify(f => f.GetByUsersAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetByOwner_NoRoom_ReturnsNull()
    {
        var result = await _sut.GetByOwnerAsync(Guid.NewGuid(), _userId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOwner_PrivateRoomForStranger_ReturnsNull()
    {
        var ownerId = Guid.NewGuid();
        var room = new Room { Id = Guid.NewGuid(), OwnerId = ownerId, Type = RoomType.Private };
        _uow.Users.Setup(u => u.GetOwnedRoomAsync(ownerId)).ReturnsAsync(room);

        var result = await _sut.GetByOwnerAsync(ownerId, _userId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOwner_PublicRoom_ReturnsCard()
    {
        var ownerId = Guid.NewGuid();
        var room = new Room { Id = Guid.NewGuid(), OwnerId = ownerId, Type = RoomType.Public };
        _uow.Users.Setup(u => u.GetOwnedRoomAsync(ownerId)).ReturnsAsync(room);

        var result = await _sut.GetByOwnerAsync(ownerId, null);

        Assert.NotNull(result);
        Assert.Equal(room.Id, result.Id);
    }

    [Fact]
    public async Task GetPublic_MarksCurrentSongsLikedByViewer()
    {
        var liked = TestData.NewSong();
        var other = TestData.NewSong();
        var likedRoom = new Room { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), CurrentSongId = liked.Id, CurrentSong = liked };
        var otherRoom = new Room { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), CurrentSongId = other.Id, CurrentSong = other };
        _uow.Rooms.Setup(r => r.GetPublicRankedAsync(1, 20)).ReturnsAsync(Page(likedRoom, otherRoom));
        _uow.Playlists
            .Setup(p => p.GetLikedSongIdsAsync(_userId, It.IsAny<ICollection<Guid>>()))
            .ReturnsAsync(new HashSet<Guid> { liked.Id });

        var result = await _sut.GetPublicAsync(1, 20, _userId);

        Assert.Equal(2, result.TotalCount);
        Assert.True(result.Items.Single(i => i.Id == likedRoom.Id).CurrentSong!.IsLiked);
        Assert.False(result.Items.Single(i => i.Id == otherRoom.Id).CurrentSong!.IsLiked);
    }

    [Fact]
    public async Task GetPublic_Anonymous_SkipsLikeLookup()
    {
        var song = TestData.NewSong();
        var room = new Room { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), CurrentSongId = song.Id, CurrentSong = song };
        _uow.Rooms.Setup(r => r.GetPublicRankedAsync(1, 20)).ReturnsAsync(Page(room));

        var result = await _sut.GetPublicAsync(1, 20, null);

        Assert.False(Assert.Single(result.Items).CurrentSong!.IsLiked);
        _uow.Playlists.Verify(p => p.GetLikedSongIdsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task GetFriendsRooms_UsesRepositoryForFriends()
    {
        var room = new Room { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Type = RoomType.Private };
        _uow.Rooms.Setup(r => r.GetByFriendsAsync(_userId, 1, 20)).ReturnsAsync(Page(room));

        var result = await _sut.GetFriendsRoomsAsync(_userId, 1, 20);

        Assert.Equal(room.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetByCurrentSong_RequestsOnlyPublicRooms()
    {
        var songId = Guid.NewGuid();
        var room = new Room { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), CurrentSongId = songId };
        _uow.Rooms.Setup(r => r.GetByCurrentSongAsync(songId, RoomType.Public, 1, 20)).ReturnsAsync(Page(room));

        var result = await _sut.GetByCurrentSongAsync(songId, 1, 20, null);

        Assert.Equal(room.Id, Assert.Single(result.Items).Id);
        _uow.Rooms.Verify(r => r.GetByCurrentSongAsync(songId, RoomType.Public, 1, 20), Times.Once);
    }

    [Fact]
    public async Task Create_InvalidType_Fails()
    {
        var result = await _sut.CreateAsync(new CreateRoomDto { Type = (RoomType)99 }, _userId);

        Assert.False(result.Success);
        Assert.Equal("Invalid room type.", result.Error);
        _uow.Rooms.Verify(r => r.AddAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task Create_UserMissing_Fails()
    {
        var result = await _sut.CreateAsync(new CreateRoomDto { Type = RoomType.Public }, _userId);

        Assert.False(result.Success);
        Assert.Equal("User not found.", result.Error);
    }

    [Fact]
    public async Task Create_AlreadyOwnsRoom_Fails()
    {
        AddUser();
        _uow.Users.Setup(u => u.GetOwnedRoomAsync(_userId)).ReturnsAsync(new Room { Id = Guid.NewGuid(), OwnerId = _userId });

        var result = await _sut.CreateAsync(new CreateRoomDto { Type = RoomType.Public }, _userId);

        Assert.False(result.Success);
        Assert.Equal("You already own a room.", result.Error);
        _uow.Rooms.Verify(r => r.AddAsync(It.IsAny<Room>()), Times.Never);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(RoomType.Public)]
    [InlineData(RoomType.Private)]
    public async Task Create_NewRoom_CreatesRoomChatAndOwnerMembership(RoomType type)
    {
        AddUser();
        Room? added = null;
        Chat? chat = null;
        _uow.Chats.Setup(c => c.AddAsync(It.IsAny<Chat>())).Callback<Chat>(c => chat = c).Returns(Task.CompletedTask);
        _uow.Rooms.Setup(r => r.AddAsync(It.IsAny<Room>())).Callback<Room>(r => added = r).Returns(Task.CompletedTask);
        _uow.Rooms.Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(() => added);

        var result = await _sut.CreateAsync(new CreateRoomDto { Type = type }, _userId);

        Assert.True(result.Success);
        Assert.NotNull(added);
        Assert.NotNull(chat);
        Assert.Equal(ChatType.Room, chat.Type);
        Assert.Equal(chat.Id, added.ChatId);
        Assert.Equal(_userId, added.OwnerId);
        Assert.Equal(type, added.Type);
        Assert.Equal(added.Id, result.Data!.Id);
        _uow.Rooms.Verify(r => r.AddMemberAsync(added.Id, _userId), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_WhileInAnotherRoom_LeavesPreviousRoomFirst()
    {
        var previous = Add(ownerId: Guid.NewGuid());
        AddUser(roomId: previous.Id);
        Room? added = null;
        _uow.Rooms.Setup(r => r.AddAsync(It.IsAny<Room>())).Callback<Room>(r => added = r).Returns(Task.CompletedTask);
        _uow.Rooms.Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(() => added);

        var result = await _sut.CreateAsync(new CreateRoomDto { Type = RoomType.Public }, _userId);

        Assert.True(result.Success);
        _uow.Rooms.Verify(r => r.RemoveMemberAsync(previous.Id, _userId), Times.Once);
        _uow.Rooms.Verify(r => r.Delete(previous), Times.Once);
    }

    [Fact]
    public async Task Delete_Missing_Fails()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid(), _userId);

        Assert.False(result.Success);
        Assert.Equal("Room not found.", result.Error);
    }

    [Fact]
    public async Task Delete_NotOwner_Fails()
    {
        var room = Add(ownerId: Guid.NewGuid());

        var result = await _sut.DeleteAsync(room.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("You are not the owner of this room.", result.Error);
        _uow.Rooms.Verify(r => r.Delete(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task Delete_Owner_RemovesRoomAndChat()
    {
        var room = Add();
        var chat = new Chat { Id = room.ChatId };
        _uow.Chats.Setup(c => c.GetByIdAsync(room.ChatId)).ReturnsAsync(chat);

        var result = await _sut.DeleteAsync(room.Id, _userId);

        Assert.True(result.Success);
        _uow.Rooms.Verify(r => r.Delete(room), Times.Once);
        _uow.Chats.Verify(c => c.Delete(chat), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Delete_ChatAlreadyGone_StillRemovesRoom()
    {
        var room = Add();

        var result = await _sut.DeleteAsync(room.Id, _userId);

        Assert.True(result.Success);
        _uow.Rooms.Verify(r => r.Delete(room), Times.Once);
        _uow.Chats.Verify(c => c.Delete(It.IsAny<Chat>()), Times.Never);
    }

    [Fact]
    public async Task Join_RoomMissing_Fails()
    {
        var result = await _sut.JoinAsync(Guid.NewGuid(), _userId);

        Assert.False(result.Success);
        Assert.Equal("Room not found.", result.Error);
    }

    [Fact]
    public async Task Join_PrivateRoomWithoutFriendship_HidesRoom()
    {
        var room = Add(ownerId: Guid.NewGuid(), type: RoomType.Private);
        AddUser();

        var result = await _sut.JoinAsync(room.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("Room not found.", result.Error);
        _uow.Rooms.Verify(r => r.AddMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Join_UserMissing_Fails()
    {
        var room = Add(ownerId: Guid.NewGuid());

        var result = await _sut.JoinAsync(room.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("User not found.", result.Error);
    }

    [Fact]
    public async Task Join_AlreadyInRoom_Fails()
    {
        var room = Add(ownerId: Guid.NewGuid());
        AddUser(roomId: room.Id);

        var result = await _sut.JoinAsync(room.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("Already in this room.", result.Error);
        _uow.Rooms.Verify(r => r.AddMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Join_PublicRoom_AddsMemberAndSaves()
    {
        var room = Add(ownerId: Guid.NewGuid());
        AddUser();

        var result = await _sut.JoinAsync(room.Id, _userId);

        Assert.True(result.Success);
        _uow.Rooms.Verify(r => r.AddMemberAsync(room.Id, _userId), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Join_PrivateRoomOfFriend_AddsMember()
    {
        var room = Add(ownerId: Guid.NewGuid(), type: RoomType.Private);
        AddUser();
        Befriend(_userId, room.OwnerId);

        var result = await _sut.JoinAsync(room.Id, _userId);

        Assert.True(result.Success);
        _uow.Rooms.Verify(r => r.AddMemberAsync(room.Id, _userId), Times.Once);
    }

    [Fact]
    public async Task Join_FromAnotherRoom_LeavesPreviousRoomFirst()
    {
        var previous = Add(ownerId: Guid.NewGuid());
        var target = Add(ownerId: Guid.NewGuid());
        AddUser(roomId: previous.Id);
        _uow.Rooms.Setup(r => r.GetRandomMemberIdAsync(previous.Id, _userId)).ReturnsAsync(Guid.NewGuid());

        var result = await _sut.JoinAsync(target.Id, _userId);

        Assert.True(result.Success);
        _uow.Rooms.Verify(r => r.RemoveMemberAsync(previous.Id, _userId), Times.Once);
        _uow.Rooms.Verify(r => r.AddMemberAsync(target.Id, _userId), Times.Once);
        _uow.Rooms.Verify(r => r.Delete(previous), Times.Never);
    }

    [Fact]
    public async Task Leave_UserMissing_Fails()
    {
        var room = Add(ownerId: Guid.NewGuid());

        var result = await _sut.LeaveAsync(room.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("You are not a member of this room.", result.Error);
    }

    [Fact]
    public async Task Leave_NotAMember_Fails()
    {
        var room = Add(ownerId: Guid.NewGuid());
        AddUser(roomId: Guid.NewGuid());

        var result = await _sut.LeaveAsync(room.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("You are not a member of this room.", result.Error);
        _uow.Rooms.Verify(r => r.RemoveMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Leave_OtherMembersRemain_KeepsRoomAndOwner()
    {
        var ownerId = Guid.NewGuid();
        var room = Add(ownerId: ownerId);
        AddUser(roomId: room.Id);
        _uow.Rooms.Setup(r => r.GetRandomMemberIdAsync(room.Id, _userId)).ReturnsAsync(ownerId);

        var result = await _sut.LeaveAsync(room.Id, _userId);

        Assert.True(result.Success);
        Assert.Equal(ownerId, room.OwnerId);
        _uow.Rooms.Verify(r => r.RemoveMemberAsync(room.Id, _userId), Times.Once);
        _uow.Rooms.Verify(r => r.Delete(It.IsAny<Room>()), Times.Never);
        _uow.Rooms.Verify(r => r.Update(It.IsAny<Room>()), Times.Never);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Leave_OwnerWithOtherMembers_PassesOwnershipToRandomMember()
    {
        var room = Add();
        var nextOwnerId = Guid.NewGuid();
        AddUser(roomId: room.Id);
        _uow.Rooms.Setup(r => r.GetRandomMemberIdAsync(room.Id, _userId)).ReturnsAsync(nextOwnerId);

        var result = await _sut.LeaveAsync(room.Id, _userId);

        Assert.True(result.Success);
        Assert.Equal(nextOwnerId, room.OwnerId);
        _uow.Rooms.Verify(r => r.Update(room), Times.Once);
        _uow.Rooms.Verify(r => r.Delete(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task Leave_LastMember_DeletesRoomAndChat()
    {
        var room = Add();
        var chat = new Chat { Id = room.ChatId };
        AddUser(roomId: room.Id);
        _uow.Chats.Setup(c => c.GetByIdAsync(room.ChatId)).ReturnsAsync(chat);

        var result = await _sut.LeaveAsync(room.Id, _userId);

        Assert.True(result.Success);
        _uow.Rooms.Verify(r => r.Delete(room), Times.Once);
        _uow.Chats.Verify(c => c.Delete(chat), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task TransferOwnership_NotOwner_Fails()
    {
        var room = Add(ownerId: Guid.NewGuid());

        var result = await _sut.TransferOwnershipAsync(room.Id, _userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("You are not the owner of this room.", result.Error);
    }

    [Fact]
    public async Task TransferOwnership_ToSelf_Fails()
    {
        var room = Add();

        var result = await _sut.TransferOwnershipAsync(room.Id, _userId, _userId);

        Assert.False(result.Success);
        Assert.Equal("You are already the owner.", result.Error);
    }

    [Fact]
    public async Task TransferOwnership_TargetMissing_Fails()
    {
        var room = Add();

        var result = await _sut.TransferOwnershipAsync(room.Id, _userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("User is not a member of this room.", result.Error);
    }

    [Fact]
    public async Task TransferOwnership_TargetNotInRoom_Fails()
    {
        var room = Add();
        var target = AddUser(Guid.NewGuid(), Guid.NewGuid());

        var result = await _sut.TransferOwnershipAsync(room.Id, _userId, target.Id);

        Assert.False(result.Success);
        Assert.Equal("User is not a member of this room.", result.Error);
        Assert.Equal(_userId, room.OwnerId);
    }

    [Fact]
    public async Task TransferOwnership_ToMember_ChangesOwnerAndSaves()
    {
        var room = Add();
        var target = AddUser(Guid.NewGuid(), room.Id);

        var result = await _sut.TransferOwnershipAsync(room.Id, _userId, target.Id);

        Assert.True(result.Success);
        Assert.Equal(target.Id, room.OwnerId);
        _uow.Rooms.Verify(r => r.Update(room), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddSong_NotOwner_Fails()
    {
        var room = Add(ownerId: Guid.NewGuid());

        var result = await _sut.AddSongAsync(room.Id, _userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("You are not the owner of this room.", result.Error);
        _uow.Rooms.Verify(r => r.AddSongAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AddSong_RoomMissing_Fails()
    {
        var result = await _sut.AddSongAsync(Guid.NewGuid(), _userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Room not found.", result.Error);
    }

    [Fact]
    public async Task AddSong_SongMissing_Fails()
    {
        var room = Add();

        var result = await _sut.AddSongAsync(room.Id, _userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Song not found.", result.Error);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddSong_EmptyRoom_BecomesCurrentSongWithoutQueueing()
    {
        var room = Add();
        var songId = Guid.NewGuid();
        room.PlaybackPositionMs = 500;
        _uow.Songs.Setup(s => s.GetByIdAsync(songId)).ReturnsAsync(new Song { Id = songId });

        var result = await _sut.AddSongAsync(room.Id, _userId, songId);

        Assert.True(result.Success);
        Assert.Equal(songId, room.CurrentSongId);
        Assert.Equal(0, room.PlaybackPositionMs);
        Assert.False(room.IsPlaying);
        _uow.Rooms.Verify(r => r.AddSongAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _uow.Rooms.Verify(r => r.Update(room), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddSong_WhilePlaying_AppendsToQueue()
    {
        var currentId = Guid.NewGuid();
        var room = Add(currentSongId: currentId);
        var songId = Guid.NewGuid();
        _uow.Songs.Setup(s => s.GetByIdAsync(songId)).ReturnsAsync(new Song { Id = songId });

        var result = await _sut.AddSongAsync(room.Id, _userId, songId);

        Assert.True(result.Success);
        Assert.Equal(currentId, room.CurrentSongId);
        _uow.Rooms.Verify(r => r.AddSongAsync(room.Id, songId), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddPlaylist_NotOwner_Fails()
    {
        var room = Add(ownerId: Guid.NewGuid());

        var result = await _sut.AddPlaylistAsync(room.Id, _userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("You are not the owner of this room.", result.Error);
    }

    [Fact]
    public async Task AddPlaylist_PlaylistMissing_Fails()
    {
        var room = Add();

        var result = await _sut.AddPlaylistAsync(room.Id, _userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Playlist not found.", result.Error);
    }

    [Fact]
    public async Task AddPlaylist_EmptyPlaylist_Fails()
    {
        var room = Add();
        var playlistId = Guid.NewGuid();
        _uow.Playlists.Setup(p => p.GetByIdAsync(playlistId)).ReturnsAsync(new Playlist { Id = playlistId });
        _uow.Playlists.Setup(p => p.GetSongIdsAsync(playlistId)).ReturnsAsync(new HashSet<Guid>());

        var result = await _sut.AddPlaylistAsync(room.Id, _userId, playlistId);

        Assert.False(result.Success);
        Assert.Equal("Playlist is empty.", result.Error);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddPlaylist_EmptyRoom_FirstSongBecomesCurrentRestQueued()
    {
        var room = Add();
        var playlistId = Guid.NewGuid();
        var songIds = NewIds(3);
        _uow.Playlists.Setup(p => p.GetByIdAsync(playlistId)).ReturnsAsync(new Playlist { Id = playlistId });
        _uow.Playlists.Setup(p => p.GetSongIdsAsync(playlistId)).ReturnsAsync(songIds);

        var result = await _sut.AddPlaylistAsync(room.Id, _userId, playlistId);

        Assert.True(result.Success);
        Assert.Contains(room.CurrentSongId!.Value, songIds);
        _uow.Rooms.Verify(r => r.AddSongsAsync(room.Id, It.Is<ICollection<Guid>>(ids => ids.Count == 2 && ids.All(songIds.Contains))), Times.Once);
        _uow.Rooms.Verify(r => r.AddSongAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddPlaylist_EmptyRoomWithTwoSongs_QueuesSingleRemainingSong()
    {
        var room = Add();
        var playlistId = Guid.NewGuid();
        var songIds = NewIds(2);
        _uow.Playlists.Setup(p => p.GetByIdAsync(playlistId)).ReturnsAsync(new Playlist { Id = playlistId });
        _uow.Playlists.Setup(p => p.GetSongIdsAsync(playlistId)).ReturnsAsync(songIds);

        var result = await _sut.AddPlaylistAsync(room.Id, _userId, playlistId);

        Assert.True(result.Success);
        _uow.Rooms.Verify(r => r.AddSongAsync(room.Id, It.Is<Guid>(id => songIds.Contains(id) && id != room.CurrentSongId)), Times.Once);
        _uow.Rooms.Verify(r => r.AddSongsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task AddPlaylist_EmptyRoomWithSingleSong_OnlySetsCurrentSong()
    {
        var room = Add();
        var playlistId = Guid.NewGuid();
        var songIds = NewIds(1);
        _uow.Playlists.Setup(p => p.GetByIdAsync(playlistId)).ReturnsAsync(new Playlist { Id = playlistId });
        _uow.Playlists.Setup(p => p.GetSongIdsAsync(playlistId)).ReturnsAsync(songIds);

        var result = await _sut.AddPlaylistAsync(room.Id, _userId, playlistId);

        Assert.True(result.Success);
        Assert.Equal(songIds.Single(), room.CurrentSongId);
        _uow.Rooms.Verify(r => r.AddSongAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _uow.Rooms.Verify(r => r.AddSongsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task AddPlaylist_WhilePlaying_QueuesAllSongsAndKeepsCurrent()
    {
        var currentId = Guid.NewGuid();
        var room = Add(currentSongId: currentId);
        var playlistId = Guid.NewGuid();
        var songIds = NewIds(3);
        _uow.Playlists.Setup(p => p.GetByIdAsync(playlistId)).ReturnsAsync(new Playlist { Id = playlistId });
        _uow.Playlists.Setup(p => p.GetSongIdsAsync(playlistId)).ReturnsAsync(songIds);

        var result = await _sut.AddPlaylistAsync(room.Id, _userId, playlistId);

        Assert.True(result.Success);
        Assert.Equal(currentId, room.CurrentSongId);
        _uow.Rooms.Verify(r => r.AddSongsAsync(room.Id, It.Is<ICollection<Guid>>(ids => ids.Count == 3)), Times.Once);
    }

    [Fact]
    public async Task AddAlbum_NotOwner_Fails()
    {
        var room = Add(ownerId: Guid.NewGuid());

        var result = await _sut.AddAlbumAsync(room.Id, _userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("You are not the owner of this room.", result.Error);
    }

    [Fact]
    public async Task AddAlbum_AlbumMissing_Fails()
    {
        var room = Add();

        var result = await _sut.AddAlbumAsync(room.Id, _userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Album not found.", result.Error);
    }

    [Fact]
    public async Task AddAlbum_EmptyAlbum_Fails()
    {
        var room = Add();
        var albumId = Guid.NewGuid();
        _uow.Albums.Setup(a => a.GetByIdAsync(albumId)).ReturnsAsync(new Album { Id = albumId });
        _uow.Albums.Setup(a => a.GetSongIdsAsync(albumId)).ReturnsAsync(new HashSet<Guid>());

        var result = await _sut.AddAlbumAsync(room.Id, _userId, albumId);

        Assert.False(result.Success);
        Assert.Equal("Album is empty.", result.Error);
    }

    [Fact]
    public async Task AddAlbum_WhilePlaying_QueuesAlbumSongs()
    {
        var room = Add(currentSongId: Guid.NewGuid());
        var albumId = Guid.NewGuid();
        var songIds = NewIds(2);
        _uow.Albums.Setup(a => a.GetByIdAsync(albumId)).ReturnsAsync(new Album { Id = albumId });
        _uow.Albums.Setup(a => a.GetSongIdsAsync(albumId)).ReturnsAsync(songIds);

        var result = await _sut.AddAlbumAsync(room.Id, _userId, albumId);

        Assert.True(result.Success);
        _uow.Rooms.Verify(r => r.AddSongsAsync(room.Id, It.Is<ICollection<Guid>>(ids => ids.Count == 2 && ids.All(songIds.Contains))), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddAlbum_EmptyRoom_FirstSongBecomesCurrent()
    {
        var room = Add();
        var albumId = Guid.NewGuid();
        var songIds = NewIds(3);
        _uow.Albums.Setup(a => a.GetByIdAsync(albumId)).ReturnsAsync(new Album { Id = albumId });
        _uow.Albums.Setup(a => a.GetSongIdsAsync(albumId)).ReturnsAsync(songIds);

        var result = await _sut.AddAlbumAsync(room.Id, _userId, albumId);

        Assert.True(result.Success);
        Assert.Contains(room.CurrentSongId!.Value, songIds);
        _uow.Rooms.Verify(r => r.AddSongsAsync(room.Id, It.Is<ICollection<Guid>>(ids => ids.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task UpdatePlayback_NotOwner_Fails()
    {
        var room = Add(ownerId: Guid.NewGuid(), currentSongId: Guid.NewGuid());

        var result = await _sut.UpdatePlaybackAsync(room.Id, _userId, new UpdatePlaybackDto { IsPlaying = true, PositionMs = 10 });

        Assert.False(result.Success);
        Assert.Equal("You are not the owner of this room.", result.Error);
        _uow.Rooms.Verify(r => r.Update(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePlayback_NothingPlaying_Fails()
    {
        var room = Add();

        var result = await _sut.UpdatePlaybackAsync(room.Id, _userId, new UpdatePlaybackDto { IsPlaying = true, PositionMs = 10 });

        Assert.False(result.Success);
        Assert.Equal("Nothing is playing.", result.Error);
    }

    [Fact]
    public async Task UpdatePlayback_NegativePosition_Fails()
    {
        var room = Add(currentSongId: Guid.NewGuid());

        var result = await _sut.UpdatePlaybackAsync(room.Id, _userId, new UpdatePlaybackDto { IsPlaying = true, PositionMs = -1 });

        Assert.False(result.Success);
        Assert.Equal("Position must be at least 0.", result.Error);
        _uow.Rooms.Verify(r => r.Update(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePlayback_Valid_StoresStateAndTouchesRoom()
    {
        var room = Add(currentSongId: Guid.NewGuid());
        var previous = DateTime.UtcNow.AddDays(-1);
        room.UpdatedAt = previous;

        var result = await _sut.UpdatePlaybackAsync(room.Id, _userId, new UpdatePlaybackDto { IsPlaying = true, PositionMs = 4200 });

        Assert.True(result.Success);
        Assert.True(room.IsPlaying);
        Assert.Equal(4200, room.PlaybackPositionMs);
        Assert.True(room.UpdatedAt > previous);
        _uow.Rooms.Verify(r => r.Update(room), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePlayback_ZeroPosition_IsAccepted()
    {
        var room = Add(currentSongId: Guid.NewGuid());

        var result = await _sut.UpdatePlaybackAsync(room.Id, _userId, new UpdatePlaybackDto { IsPlaying = false, PositionMs = 0 });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Skip_NotOwner_Fails()
    {
        var room = Add(ownerId: Guid.NewGuid());

        var result = await _sut.SkipAsync(room.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("You are not the owner of this room.", result.Error);
        _uow.Rooms.Verify(r => r.DequeueAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Skip_QueueHasNext_MovesToNextSongAndKeepsPlayingState()
    {
        var room = Add(currentSongId: Guid.NewGuid());
        var nextSongId = Guid.NewGuid();
        room.IsPlaying = true;
        room.PlaybackPositionMs = 9000;
        _uow.Rooms.Setup(r => r.DequeueAsync(room.Id)).ReturnsAsync(new QueueItem { Id = Guid.NewGuid(), RoomId = room.Id, SongId = nextSongId });

        var result = await _sut.SkipAsync(room.Id, _userId);

        Assert.True(result.Success);
        Assert.Equal(nextSongId, room.CurrentSongId);
        Assert.Equal(0, room.PlaybackPositionMs);
        Assert.True(room.IsPlaying);
        _uow.Rooms.Verify(r => r.Update(room), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Skip_QueueEmpty_ClearsCurrentSongAndStopsPlayback()
    {
        var room = Add(currentSongId: Guid.NewGuid());
        room.IsPlaying = true;
        room.PlaybackPositionMs = 9000;

        var result = await _sut.SkipAsync(room.Id, _userId);

        Assert.True(result.Success);
        Assert.Null(room.CurrentSongId);
        Assert.Equal(0, room.PlaybackPositionMs);
        Assert.False(room.IsPlaying);
    }
}

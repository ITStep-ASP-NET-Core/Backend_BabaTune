using BabaTune.Application.Common;
using BabaTune.Application.DTO.Rooms;
using BabaTune.Application.Interfaces;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using BabaTune.WebApi.Controllers;
using BabaTune.WebApi.Requests;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BabaTune.Tests.Controllers;

public class RoomsControllerTests
{
    private readonly Mock<IRoomService> _service = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _roomId = Guid.NewGuid();
    private readonly RoomsController _sut;

    public RoomsControllerTests()
    {
        _sut = new RoomsController(_service.Object).WithUser(_userId);
    }

    private static void AssertStatus(IActionResult result, int status)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(status, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetPublic_Anonymous_ClampsPagingAndPassesNullViewer()
    {
        var anonymous = new RoomsController(_service.Object).WithUser(null);
        _service.Setup(s => s.GetPublicAsync(1, 100, null)).ReturnsAsync(new PagedResult<RoomCardDto>());

        var result = await anonymous.GetPublic(0, 1000);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetPublicAsync(1, 100, null), Times.Once);
    }

    [Fact]
    public async Task GetPublic_Authenticated_PassesCurrentUser()
    {
        _service.Setup(s => s.GetPublicAsync(1, 20, _userId)).ReturnsAsync(new PagedResult<RoomCardDto>());

        var result = await _sut.GetPublic();

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetPublicAsync(1, 20, _userId), Times.Once);
    }

    [Fact]
    public async Task GetFriendsRooms_UsesCurrentUser()
    {
        _service.Setup(s => s.GetFriendsRoomsAsync(_userId, 1, 20)).ReturnsAsync(new PagedResult<RoomCardDto>());

        var result = await _sut.GetFriendsRooms();

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetFriendsRoomsAsync(_userId, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        _service.Setup(s => s.GetByIdAsync(_roomId, _userId)).ReturnsAsync((RoomDto?)null);

        var result = await _sut.GetById(_roomId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_Found_ReturnsOk()
    {
        var room = new RoomDto { Id = _roomId };
        _service.Setup(s => s.GetByIdAsync(_roomId, _userId)).ReturnsAsync(room);

        var result = await _sut.GetById(_roomId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(room, ok.Value);
    }

    [Fact]
    public async Task GetByOwner_Missing_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        _service.Setup(s => s.GetByOwnerAsync(ownerId, _userId)).ReturnsAsync((RoomCardDto?)null);

        var result = await _sut.GetByOwner(ownerId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetByOwner_Found_ReturnsOk()
    {
        var ownerId = Guid.NewGuid();
        _service.Setup(s => s.GetByOwnerAsync(ownerId, _userId)).ReturnsAsync(new RoomCardDto { Id = _roomId });

        var result = await _sut.GetByOwner(ownerId);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetByCurrentSong_PassesSongAndViewer()
    {
        var songId = Guid.NewGuid();
        _service.Setup(s => s.GetByCurrentSongAsync(songId, 1, 20, _userId)).ReturnsAsync(new PagedResult<RoomCardDto>());

        var result = await _sut.GetByCurrentSong(songId);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetByCurrentSongAsync(songId, 1, 20, _userId), Times.Once);
    }

    [Fact]
    public async Task Create_Success_Returns201WithRoom()
    {
        var dto = new CreateRoomDto { Type = RoomType.Public };
        var room = new RoomDto { Id = _roomId };
        _service.Setup(s => s.CreateAsync(dto, _userId)).ReturnsAsync(Result.Ok(room));

        var result = await _sut.Create(dto);

        var created = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Same(room, created.Value);
    }

    [Theory]
    [InlineData("You already own a room.", 409)]
    [InlineData("Invalid room type.", 400)]
    [InlineData("User not found.", 404)]
    public async Task Create_Failure_MapsStatus(string error, int expectedStatus)
    {
        var dto = new CreateRoomDto();
        _service.Setup(s => s.CreateAsync(dto, _userId)).ReturnsAsync(Result<RoomDto>.Fail(error));

        var result = await _sut.Create(dto);

        AssertStatus(result, expectedStatus);
    }

    [Fact]
    public async Task Delete_Success_ReturnsNoContent()
    {
        _service.Setup(s => s.DeleteAsync(_roomId, _userId, false)).ReturnsAsync(Result.Ok());

        var result = await _sut.Delete(_roomId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NotOwner_Returns403()
    {
        _service.Setup(s => s.DeleteAsync(_roomId, _userId, false)).ReturnsAsync(Result.Fail("You are not the owner of this room."));

        var result = await _sut.Delete(_roomId);

        AssertStatus(result, 403);
    }

    [Fact]
    public async Task Join_Success_ReturnsNoContent()
    {
        _service.Setup(s => s.JoinAsync(_roomId, _userId)).ReturnsAsync(Result.Ok());

        var result = await _sut.Join(_roomId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Join_AlreadyInRoom_Returns409()
    {
        _service.Setup(s => s.JoinAsync(_roomId, _userId)).ReturnsAsync(Result.Fail("Already in this room."));

        var result = await _sut.Join(_roomId);

        AssertStatus(result, 409);
    }

    [Fact]
    public async Task Join_RoomHidden_Returns404()
    {
        _service.Setup(s => s.JoinAsync(_roomId, _userId)).ReturnsAsync(Result.Fail("Room not found."));

        var result = await _sut.Join(_roomId);

        AssertStatus(result, 404);
    }

    [Fact]
    public async Task Leave_Success_ReturnsNoContent()
    {
        _service.Setup(s => s.LeaveAsync(_roomId, _userId)).ReturnsAsync(Result.Ok());

        var result = await _sut.Leave(_roomId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task TransferOwnership_PassesNewOwnerFromBody()
    {
        var newOwnerId = Guid.NewGuid();
        _service.Setup(s => s.TransferOwnershipAsync(_roomId, _userId, newOwnerId)).ReturnsAsync(Result.Ok());

        var result = await _sut.TransferOwnership(_roomId, new TransferOwnershipRequest { NewOwnerId = newOwnerId });

        Assert.IsType<NoContentResult>(result);
        _service.Verify(s => s.TransferOwnershipAsync(_roomId, _userId, newOwnerId), Times.Once);
    }

    [Fact]
    public async Task TransferOwnership_NotOwner_Returns403()
    {
        var newOwnerId = Guid.NewGuid();
        _service
            .Setup(s => s.TransferOwnershipAsync(_roomId, _userId, newOwnerId))
            .ReturnsAsync(Result.Fail("You are not the owner of this room."));

        var result = await _sut.TransferOwnership(_roomId, new TransferOwnershipRequest { NewOwnerId = newOwnerId });

        AssertStatus(result, 403);
    }

    [Fact]
    public async Task AddSong_PassesIdsAndReturnsNoContent()
    {
        var songId = Guid.NewGuid();
        _service.Setup(s => s.AddSongAsync(_roomId, _userId, songId)).ReturnsAsync(Result.Ok());

        var result = await _sut.AddSong(_roomId, songId);

        Assert.IsType<NoContentResult>(result);
        _service.Verify(s => s.AddSongAsync(_roomId, _userId, songId), Times.Once);
    }

    [Fact]
    public async Task AddSong_SongMissing_Returns404()
    {
        var songId = Guid.NewGuid();
        _service.Setup(s => s.AddSongAsync(_roomId, _userId, songId)).ReturnsAsync(Result.Fail("Song not found."));

        var result = await _sut.AddSong(_roomId, songId);

        AssertStatus(result, 404);
    }

    [Fact]
    public async Task AddPlaylist_PassesIdsAndReturnsNoContent()
    {
        var playlistId = Guid.NewGuid();
        _service.Setup(s => s.AddPlaylistAsync(_roomId, _userId, playlistId)).ReturnsAsync(Result.Ok());

        var result = await _sut.AddPlaylist(_roomId, playlistId);

        Assert.IsType<NoContentResult>(result);
        _service.Verify(s => s.AddPlaylistAsync(_roomId, _userId, playlistId), Times.Once);
    }

    [Fact]
    public async Task AddPlaylist_Empty_Returns400()
    {
        var playlistId = Guid.NewGuid();
        _service.Setup(s => s.AddPlaylistAsync(_roomId, _userId, playlistId)).ReturnsAsync(Result.Fail("Playlist is empty."));

        var result = await _sut.AddPlaylist(_roomId, playlistId);

        AssertStatus(result, 400);
    }

    [Fact]
    public async Task AddAlbum_PassesIdsAndReturnsNoContent()
    {
        var albumId = Guid.NewGuid();
        _service.Setup(s => s.AddAlbumAsync(_roomId, _userId, albumId)).ReturnsAsync(Result.Ok());

        var result = await _sut.AddAlbum(_roomId, albumId);

        Assert.IsType<NoContentResult>(result);
        _service.Verify(s => s.AddAlbumAsync(_roomId, _userId, albumId), Times.Once);
    }

    [Fact]
    public async Task AddAlbum_AlbumMissing_Returns404()
    {
        var albumId = Guid.NewGuid();
        _service.Setup(s => s.AddAlbumAsync(_roomId, _userId, albumId)).ReturnsAsync(Result.Fail("Album not found."));

        var result = await _sut.AddAlbum(_roomId, albumId);

        AssertStatus(result, 404);
    }

    [Fact]
    public async Task UpdatePlayback_PassesBodyAndReturnsNoContent()
    {
        var dto = new UpdatePlaybackDto { IsPlaying = true, PositionMs = 1500 };
        _service.Setup(s => s.UpdatePlaybackAsync(_roomId, _userId, dto)).ReturnsAsync(Result.Ok());

        var result = await _sut.UpdatePlayback(_roomId, dto);

        Assert.IsType<NoContentResult>(result);
        _service.Verify(s => s.UpdatePlaybackAsync(_roomId, _userId, dto), Times.Once);
    }

    [Fact]
    public async Task UpdatePlayback_NothingPlaying_Returns400()
    {
        var dto = new UpdatePlaybackDto();
        _service.Setup(s => s.UpdatePlaybackAsync(_roomId, _userId, dto)).ReturnsAsync(Result.Fail("Nothing is playing."));

        var result = await _sut.UpdatePlayback(_roomId, dto);

        AssertStatus(result, 400);
    }

    [Fact]
    public async Task Skip_Success_ReturnsNoContent()
    {
        _service.Setup(s => s.SkipAsync(_roomId, _userId)).ReturnsAsync(Result.Ok());

        var result = await _sut.Skip(_roomId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Skip_NotOwner_Returns403()
    {
        _service.Setup(s => s.SkipAsync(_roomId, _userId)).ReturnsAsync(Result.Fail("You are not the owner of this room."));

        var result = await _sut.Skip(_roomId);

        AssertStatus(result, 403);
    }
}

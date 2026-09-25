using BabaTune.Application.DTO.Playlists;
using BabaTune.Application.Implementations;
using BabaTune.Application.Interfaces;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Moq;

namespace BabaTune.Tests.Services;

public class PlaylistServiceTests
{
    private readonly UowMock _uow = new();
    private readonly Mock<IListenHistoryService> _history = new();
    private readonly PlaylistService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public PlaylistServiceTests()
    {
        _sut = new PlaylistService(_uow.Object, _history.Object);
    }

    private Playlist Add(PlaylistType type = PlaylistType.Custom, Guid? ownerId = null, string image = Defaults.PlaylistImage)
    {
        var playlist = new Playlist
        {
            Id = Guid.NewGuid(),
            UserId = ownerId ?? _userId,
            Type = type,
            Name = "Playlist",
            ImageUrl = image
        };
        _uow.Playlists.Setup(p => p.GetByIdAsync(playlist.Id)).ReturnsAsync(playlist);
        return playlist;
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task Create_WithoutImage_UsesDefaultImageAndSkipsStorage()
    {
        Playlist? added = null;
        _uow.Playlists.Setup(p => p.AddAsync(It.IsAny<Playlist>())).Callback<Playlist>(p => added = p).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new CreatePlaylistDto(), _userId);

        Assert.True(result.Success);
        Assert.NotNull(added);
        Assert.Equal(_userId, added.UserId);
        Assert.Equal(Defaults.PlaylistImage, added.ImageUrl);
        _uow.Storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_WithImage_UploadsToCoversFolder()
    {
        Playlist? added = null;
        _uow.Playlists.Setup(p => p.AddAsync(It.IsAny<Playlist>())).Callback<Playlist>(p => added = p).Returns(Task.CompletedTask);
        _uow.Storage
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), "cover.png", "image/png", "playlists/covers"))
            .ReturnsAsync("https://cloud/cover.png");

        var result = await _sut.CreateAsync(new CreatePlaylistDto { ImageFile = FormFiles.Create("cover.png", "image/png") }, _userId);

        Assert.True(result.Success);
        Assert.NotNull(added);
        Assert.Equal("https://cloud/cover.png", added.ImageUrl);
    }

    [Fact]
    public async Task UpdateInfo_NotFound_Fails()
    {
        var result = await _sut.UpdateInfoAsync(Guid.NewGuid(), _userId, new UpdatePlaylistDto { Name = "N" });

        Assert.False(result.Success);
        Assert.Equal("Playlist not found.", result.Error);
    }

    [Fact]
    public async Task UpdateInfo_NotOwner_Fails()
    {
        var playlist = Add(ownerId: Guid.NewGuid());

        var result = await _sut.UpdateInfoAsync(playlist.Id, _userId, new UpdatePlaylistDto { Name = "N" });

        Assert.False(result.Success);
        Assert.Equal("You are not the owner of this playlist.", result.Error);
        _uow.Playlists.Verify(p => p.Update(It.IsAny<Playlist>()), Times.Never);
    }

    [Fact]
    public async Task UpdateInfo_LikedPlaylist_CannotBeRenamed()
    {
        var playlist = Add(PlaylistType.Liked);

        var result = await _sut.UpdateInfoAsync(playlist.Id, _userId, new UpdatePlaylistDto { Name = "N" });

        Assert.False(result.Success);
        Assert.Equal("Liked playlist cannot be renamed.", result.Error);
    }

    [Fact]
    public async Task UpdateInfo_NullName_KeepsExistingName()
    {
        var playlist = Add();

        var result = await _sut.UpdateInfoAsync(playlist.Id, _userId, new UpdatePlaylistDto { Name = null });

        Assert.True(result.Success);
        Assert.Equal("Playlist", playlist.Name);
        _uow.Playlists.Verify(p => p.Update(playlist), Times.Once);
    }

    [Fact]
    public async Task UpdateImage_NewFile_DeletesOldAndUploadsNew()
    {
        var playlist = Add(image: "https://cloud/old.png");
        _uow.Storage
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), "new.png", "image/png", "playlists/covers"))
            .ReturnsAsync("https://cloud/new.png");

        var result = await _sut.UpdateImageAsync(playlist.Id, _userId, FormFiles.Create("new.png", "image/png"));

        Assert.True(result.Success);
        Assert.Equal("https://cloud/new.png", playlist.ImageUrl);
        _uow.Storage.Verify(s => s.DeleteAsync("https://cloud/old.png"), Times.Once);
    }

    [Fact]
    public async Task UpdateImage_NullFile_ResetsToDefault()
    {
        var playlist = Add(image: "https://cloud/old.png");

        var result = await _sut.UpdateImageAsync(playlist.Id, _userId, null);

        Assert.True(result.Success);
        Assert.Equal(Defaults.PlaylistImage, playlist.ImageUrl);
        _uow.Storage.Verify(s => s.DeleteAsync("https://cloud/old.png"), Times.Once);
        _uow.Storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateImage_DefaultImage_IsNotDeletedFromStorage()
    {
        var playlist = Add();

        await _sut.UpdateImageAsync(playlist.Id, _userId, null);

        _uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_NotOwner_Fails()
    {
        var playlist = Add(ownerId: Guid.NewGuid());

        var result = await _sut.DeleteAsync(playlist.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("You are not the owner of this playlist.", result.Error);
        _uow.Playlists.Verify(p => p.Delete(It.IsAny<Playlist>()), Times.Never);
    }

    [Fact]
    public async Task Delete_LikedPlaylist_Fails()
    {
        var playlist = Add(PlaylistType.Liked);

        var result = await _sut.DeleteAsync(playlist.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("Liked playlist cannot be deleted.", result.Error);
        _uow.Playlists.Verify(p => p.Delete(It.IsAny<Playlist>()), Times.Never);
    }

    [Fact]
    public async Task Delete_CustomImage_RemovesFileFromStorage()
    {
        var playlist = Add(image: "https://cloud/custom.png");

        var result = await _sut.DeleteAsync(playlist.Id, _userId);

        Assert.True(result.Success);
        _uow.Storage.Verify(s => s.DeleteAsync("https://cloud/custom.png"), Times.Once);
        _uow.Playlists.Verify(p => p.Delete(playlist), Times.Once);
    }

    [Fact]
    public async Task Delete_DefaultImage_DoesNotTouchStorage()
    {
        var playlist = Add();

        var result = await _sut.DeleteAsync(playlist.Id, _userId);

        Assert.True(result.Success);
        _uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddSong_NotOwner_Fails()
    {
        var playlist = Add(ownerId: Guid.NewGuid());

        var result = await _sut.AddSongAsync(playlist.Id, _userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("You are not the owner of this playlist.", result.Error);
        _uow.Playlists.Verify(p => p.AddSongAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AddSong_SongMissing_Fails()
    {
        var playlist = Add();

        var result = await _sut.AddSongAsync(playlist.Id, _userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Song not found.", result.Error);
    }

    [Fact]
    public async Task AddSong_ToLikedPlaylist_SyncsListenHistory()
    {
        var playlist = Add(PlaylistType.Liked);
        var songId = Guid.NewGuid();
        _uow.Songs.Setup(s => s.GetByIdAsync(songId)).ReturnsAsync(new Song { Id = songId });

        var result = await _sut.AddSongAsync(playlist.Id, _userId, songId);

        Assert.True(result.Success);
        _history.Verify(h => h.SyncLikeAsync(_userId, songId, true), Times.Once);
        _uow.Playlists.Verify(p => p.AddSongAsync(playlist.Id, songId), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddSong_ToCustomPlaylist_DoesNotSyncListenHistory()
    {
        var playlist = Add();
        var songId = Guid.NewGuid();
        _uow.Songs.Setup(s => s.GetByIdAsync(songId)).ReturnsAsync(new Song { Id = songId });

        var result = await _sut.AddSongAsync(playlist.Id, _userId, songId);

        Assert.True(result.Success);
        _history.Verify(h => h.SyncLikeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        _uow.Playlists.Verify(p => p.AddSongAsync(playlist.Id, songId), Times.Once);
    }

    [Fact]
    public async Task RemoveSong_FromLikedPlaylist_ClearsLikeInHistory()
    {
        var playlist = Add(PlaylistType.Liked);
        var songId = Guid.NewGuid();

        var result = await _sut.RemoveSongAsync(playlist.Id, _userId, songId);

        Assert.True(result.Success);
        _history.Verify(h => h.SyncLikeAsync(_userId, songId, false), Times.Once);
        _uow.Playlists.Verify(p => p.RemoveSongAsync(playlist.Id, songId), Times.Once);
    }

    [Fact]
    public async Task RemoveSong_FromCustomPlaylist_DoesNotSyncListenHistory()
    {
        var playlist = Add();

        var result = await _sut.RemoveSongAsync(playlist.Id, _userId, Guid.NewGuid());

        Assert.True(result.Success);
        _history.Verify(h => h.SyncLikeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task MoveSong_SongNotInPlaylist_Fails()
    {
        var playlist = Add();

        var result = await _sut.MoveSongAsync(playlist.Id, _userId, Guid.NewGuid(), 2);

        Assert.False(result.Success);
        Assert.Equal("Song is not in this playlist.", result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task MoveSong_PositionBelowOne_Fails(int position)
    {
        var playlist = Add();
        var songId = Guid.NewGuid();
        _uow.Playlists.Setup(p => p.ContainsSongAsync(playlist.Id, songId)).ReturnsAsync(true);

        var result = await _sut.MoveSongAsync(playlist.Id, _userId, songId, position);

        Assert.False(result.Success);
        Assert.Equal("Position must be at least 1.", result.Error);
        _uow.Playlists.Verify(p => p.MoveSongAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task MoveSong_ValidPosition_MovesAndSaves()
    {
        var playlist = Add();
        var songId = Guid.NewGuid();
        _uow.Playlists.Setup(p => p.ContainsSongAsync(playlist.Id, songId)).ReturnsAsync(true);

        var result = await _sut.MoveSongAsync(playlist.Id, _userId, songId, 3);

        Assert.True(result.Success);
        _uow.Playlists.Verify(p => p.MoveSongAsync(playlist.Id, songId, 3), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}

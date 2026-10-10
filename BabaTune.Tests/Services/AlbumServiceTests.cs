using BabaTune.Application.Common;
using BabaTune.Application.DTO.Albums;
using BabaTune.Application.Implementations;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Moq;
using static BabaTune.Application.Common.Constatnts;

namespace BabaTune.Tests.Services;

public class AlbumServiceTests
{
	private readonly UowMock _uow = new();
	private readonly AlbumService _sut;
	private readonly Guid _userId = Guid.NewGuid();

	public AlbumServiceTests ( )
	{
		_sut = new AlbumService(_uow.Object);
	}

	private Album Add ( Guid? ownerId = null, string image = Defaults.AlbumImage )
	{
		var album = new Album
		{
			Id = Guid.NewGuid(),
			Name = "Album",
			Description = "Description",
			UserId = ownerId ?? _userId,
			ImageUrl = image
		};
		_uow.Albums.Setup(a => a.GetByIdAsync(album.Id)).ReturnsAsync(album);
		return album;
	}

	private void AllSongsOwned ( )
	{
		_uow.Albums
			.Setup(a => a.CountOwnedSongsAsync(_userId, It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(( Guid _, ICollection<Guid> ids ) => ids.Count);
	}

	private static List<Guid> NewIds ( int count ) => Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

	[Fact]
	public async Task GetById_Missing_ReturnsNull ( )
	{
		var result = await _sut.GetByIdAsync(Guid.NewGuid());

		Assert.Null(result);
	}

	[Fact]
	public async Task Create_NoSongs_FailsWithoutTouchingStorageOrRepository ( )
	{
		var result = await _sut.CreateAsync(new CreateAlbumDto { SongIds = [] }, _userId);

		Assert.False(result.Success);
		Assert.Equal("Album must contain at least one song.", result.Error);
		_uow.Albums.Verify(a => a.AddAsync(It.IsAny<Album>()), Times.Never);
		_uow.Storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
	}

	[Fact]
	public async Task Create_TooManySongs_Fails ( )
	{
		var result = await _sut.CreateAsync(new CreateAlbumDto { SongIds = NewIds(Limits.MaxAlbumSongs + 1) }, _userId);

		Assert.False(result.Success);
		Assert.Equal("Album is full.", result.Error);
		_uow.Albums.Verify(a => a.AddAsync(It.IsAny<Album>()), Times.Never);
	}

	[Fact]
	public async Task Create_ForeignSongs_FailsBeforeUpload ( )
	{
		var dto = new CreateAlbumDto
		{
			SongIds = NewIds(2),
			ImageFile = FormFiles.Create("c.png", "image/png")
		};
		_uow.Albums
			.Setup(a => a.CountOwnedSongsAsync(_userId, It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(1);

		var result = await _sut.CreateAsync(dto, _userId);

		Assert.False(result.Success);
		Assert.Equal("You are not the author of these songs.", result.Error);
		_uow.Storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
		_uow.Albums.Verify(a => a.AddAsync(It.IsAny<Album>()), Times.Never);
	}

	[Fact]
	public async Task Create_SongAlreadyInAlbum_Fails ( )
	{
		AllSongsOwned();
		_uow.Albums.Setup(a => a.AnySongInAlbumAsync(It.IsAny<ICollection<Guid>>())).ReturnsAsync(true);

		var result = await _sut.CreateAsync(new CreateAlbumDto { SongIds = NewIds(1) }, _userId);

		Assert.False(result.Success);
		Assert.Equal("Song is already in an album.", result.Error);
		_uow.Albums.Verify(a => a.AddAsync(It.IsAny<Album>()), Times.Never);
	}

	[Fact]
	public async Task Create_WithoutImage_UsesDefaultCoverAndAttachesDistinctSongs ( )
	{
		Album? added = null;
		var songId = Guid.NewGuid();
		AllSongsOwned();
		_uow.Albums.Setup(a => a.AddAsync(It.IsAny<Album>())).Callback<Album>(a => added = a).Returns(Task.CompletedTask);

		var result = await _sut.CreateAsync(new CreateAlbumDto { SongIds = [songId, songId] }, _userId);

		Assert.True(result.Success);
		Assert.NotNull(added);
		Assert.Equal(_userId, added.UserId);
		Assert.Equal(Defaults.AlbumImage, added.ImageUrl);
		_uow.Albums.Verify(a => a.AddSongsAsync(added.Id, It.Is<ICollection<Guid>>(ids => ids.Count == 1 && ids.Contains(songId))), Times.Once);
		_uow.Storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task Create_WithImage_UploadsCoverToCoversFolder ( )
	{
		Album? added = null;
		AllSongsOwned();
		_uow.Albums.Setup(a => a.AddAsync(It.IsAny<Album>())).Callback<Album>(a => added = a).Returns(Task.CompletedTask);
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "c.png", "image/png", "albums/covers"))
			.ReturnsAsync("https://cloud/c.png");

		var result = await _sut.CreateAsync(new CreateAlbumDto
		{
			SongIds = NewIds(1),
			ImageFile = FormFiles.Create("c.png", "image/png")
		}, _userId);

		Assert.True(result.Success);
		Assert.NotNull(added);
		Assert.Equal("https://cloud/c.png", added.ImageUrl);
	}

	[Fact]
	public async Task Create_WhenSaveFails_RemovesUploadedCover ( )
	{
		AllSongsOwned();
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "c.png", "image/png", "albums/covers"))
			.ReturnsAsync("https://cloud/c.png");
		_uow.Mock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());

		await Assert.ThrowsAsync<InvalidOperationException>(( ) => _sut.CreateAsync(new CreateAlbumDto
		{
			SongIds = NewIds(1),
			ImageFile = FormFiles.Create("c.png", "image/png")
		}, _userId));

		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/c.png"), Times.Once);
	}

	[Fact]
	public async Task Create_WhenSaveFailsWithoutCover_DoesNotTouchStorage ( )
	{
		AllSongsOwned();
		_uow.Mock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());

		await Assert.ThrowsAsync<InvalidOperationException>(( ) => _sut.CreateAsync(new CreateAlbumDto { SongIds = NewIds(1) }, _userId));

		_uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task UpdateInfo_NotAuthor_Fails ( )
	{
		var album = Add(ownerId: Guid.NewGuid());

		var result = await _sut.UpdateInfoAsync(album.Id, _userId, new UpdateAlbumDto { Name = "X" });

		Assert.False(result.Success);
		Assert.Equal("You are not the author of this album.", result.Error);
		_uow.Albums.Verify(a => a.Update(It.IsAny<Album>()), Times.Never);
	}

	[Fact]
	public async Task UpdateInfo_PartialDto_KeepsUntouchedFields ( )
	{
		var album = Add();

		var result = await _sut.UpdateInfoAsync(album.Id, _userId, new UpdateAlbumDto { Name = null, Description = "New description" });

		Assert.True(result.Success);
		Assert.Equal("Album", album.Name);
		Assert.Equal("New description", album.Description);
	}

	[Fact]
	public async Task UpdateImage_Reset_DeletesCustomCoverAndRestoresDefault ( )
	{
		var album = Add(image: "https://cloud/old.png");

		var result = await _sut.UpdateImageAsync(album.Id, _userId, null);

		Assert.True(result.Success);
		Assert.Equal(Defaults.AlbumImage, album.ImageUrl);
		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/old.png"), Times.Once);
	}

	[Fact]
	public async Task AddSong_AlbumMissing_Fails ( )
	{
		var result = await _sut.AddSongAsync(Guid.NewGuid(), _userId, Guid.NewGuid());

		Assert.False(result.Success);
		Assert.Equal("Album not found.", result.Error);
	}

	[Fact]
	public async Task AddSong_NotAuthor_Fails ( )
	{
		var album = Add(ownerId: Guid.NewGuid());

		var result = await _sut.AddSongAsync(album.Id, _userId, Guid.NewGuid());

		Assert.False(result.Success);
		Assert.Equal("You are not the author of this album.", result.Error);
		_uow.Albums.Verify(a => a.AddSongsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
	}

	[Fact]
	public async Task AddSong_ForeignSong_Fails ( )
	{
		var album = Add();
		_uow.Albums.Setup(a => a.CountOwnedSongsAsync(_userId, It.IsAny<ICollection<Guid>>())).ReturnsAsync(0);

		var result = await _sut.AddSongAsync(album.Id, _userId, Guid.NewGuid());

		Assert.False(result.Success);
		Assert.Equal("You are not the author of these songs.", result.Error);
		_uow.Albums.Verify(a => a.AddSongsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
	}

	[Fact]
	public async Task AddSong_SongInAnotherAlbum_Fails ( )
	{
		var album = Add();
		AllSongsOwned();
		_uow.Albums.Setup(a => a.AnySongInAlbumAsync(It.IsAny<ICollection<Guid>>())).ReturnsAsync(true);

		var result = await _sut.AddSongAsync(album.Id, _userId, Guid.NewGuid());

		Assert.False(result.Success);
		Assert.Equal("Song is already in an album.", result.Error);
		_uow.Albums.Verify(a => a.AddSongsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
	}

	[Fact]
	public async Task AddSong_AlbumFull_Fails ( )
	{
		var album = Add();
		AllSongsOwned();
		_uow.Albums.Setup(a => a.GetSongsCountAsync(album.Id)).ReturnsAsync(Limits.MaxAlbumSongs);

		var result = await _sut.AddSongAsync(album.Id, _userId, Guid.NewGuid());

		Assert.False(result.Success);
		Assert.Equal("Album is full.", result.Error);
	}

	[Fact]
	public async Task AddSong_Valid_AddsSingleSongAndSaves ( )
	{
		var album = Add();
		var songId = Guid.NewGuid();
		AllSongsOwned();

		var result = await _sut.AddSongAsync(album.Id, _userId, songId);

		Assert.True(result.Success);
		_uow.Albums.Verify(a => a.AddSongsAsync(album.Id, It.Is<ICollection<Guid>>(ids => ids.Count == 1 && ids.Contains(songId))), Times.Once);
		_uow.Albums.Verify(a => a.Update(album), Times.Once);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task AddSongs_AlbumMissing_Fails ( )
	{
		var result = await _sut.AddSongsAsync(Guid.NewGuid(), _userId, NewIds(1));

		Assert.False(result.Success);
		Assert.Equal("Album not found.", result.Error);
	}

	[Fact]
	public async Task AddSongs_NotAuthor_Fails ( )
	{
		var album = Add(ownerId: Guid.NewGuid());

		var result = await _sut.AddSongsAsync(album.Id, _userId, NewIds(1));

		Assert.False(result.Success);
		Assert.Equal("You are not the author of this album.", result.Error);
	}

	[Fact]
	public async Task AddSongs_EmptyList_Fails ( )
	{
		var album = Add();

		var result = await _sut.AddSongsAsync(album.Id, _userId, []);

		Assert.False(result.Success);
		Assert.Equal("No songs provided.", result.Error);
		_uow.Albums.Verify(a => a.AddSongsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
	}

	[Fact]
	public async Task AddSongs_ExceedsLimitWithExistingSongs_Fails ( )
	{
		var album = Add();
		AllSongsOwned();
		_uow.Albums.Setup(a => a.GetSongsCountAsync(album.Id)).ReturnsAsync(Limits.MaxAlbumSongs - 1);

		var result = await _sut.AddSongsAsync(album.Id, _userId, NewIds(2));

		Assert.False(result.Success);
		Assert.Equal("Album is full.", result.Error);
		_uow.Albums.Verify(a => a.AddSongsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
	}

	[Fact]
	public async Task AddSongs_ForeignSong_Fails ( )
	{
		var album = Add();
		_uow.Albums.Setup(a => a.CountOwnedSongsAsync(_userId, It.IsAny<ICollection<Guid>>())).ReturnsAsync(1);

		var result = await _sut.AddSongsAsync(album.Id, _userId, NewIds(2));

		Assert.False(result.Success);
		Assert.Equal("You are not the author of these songs.", result.Error);
	}

	[Fact]
	public async Task AddSongs_Valid_AddsDistinctSongsAndTouchesAlbum ( )
	{
		var album = Add();
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		var previous = DateTime.UtcNow.AddDays(-1);
		album.UpdatedAt = previous;
		AllSongsOwned();

		var result = await _sut.AddSongsAsync(album.Id, _userId, [first, second, first]);

		Assert.True(result.Success);
		Assert.True(album.UpdatedAt > previous);
		_uow.Albums.Verify(a => a.AddSongsAsync(album.Id, It.Is<ICollection<Guid>>(ids => ids.Count == 2 && ids.Contains(first) && ids.Contains(second))), Times.Once);
		_uow.Albums.Verify(a => a.Update(album), Times.Once);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task RemoveSong_AlbumMissing_Fails ( )
	{
		var result = await _sut.RemoveSongAsync(Guid.NewGuid(), _userId, Guid.NewGuid());

		Assert.False(result.Success);
		Assert.Equal("Album not found.", result.Error);
	}

	[Fact]
	public async Task RemoveSong_NotAuthor_Fails ( )
	{
		var album = Add(ownerId: Guid.NewGuid());

		var result = await _sut.RemoveSongAsync(album.Id, _userId, Guid.NewGuid());

		Assert.False(result.Success);
		Assert.Equal("You are not the author of this album.", result.Error);
		_uow.Albums.Verify(a => a.RemoveSongAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
	}

	[Fact]
	public async Task RemoveSong_SongNotInAlbum_Fails ( )
	{
		var album = Add();

		var result = await _sut.RemoveSongAsync(album.Id, _userId, Guid.NewGuid());

		Assert.False(result.Success);
		Assert.Equal("Song is not in this album.", result.Error);
		_uow.Albums.Verify(a => a.RemoveSongAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
	}

	[Fact]
	public async Task RemoveSong_LastSong_Fails ( )
	{
		var album = Add();
		var songId = Guid.NewGuid();
		_uow.Albums.Setup(a => a.ContainsSongAsync(album.Id, songId)).ReturnsAsync(true);
		_uow.Albums.Setup(a => a.GetSongsCountAsync(album.Id)).ReturnsAsync(1);

		var result = await _sut.RemoveSongAsync(album.Id, _userId, songId);

		Assert.False(result.Success);
		Assert.Equal("Album must contain at least one song.", result.Error);
		_uow.Albums.Verify(a => a.RemoveSongAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
	}

	[Fact]
	public async Task RemoveSong_Valid_RemovesAndSaves ( )
	{
		var album = Add();
		var songId = Guid.NewGuid();
		_uow.Albums.Setup(a => a.ContainsSongAsync(album.Id, songId)).ReturnsAsync(true);
		_uow.Albums.Setup(a => a.GetSongsCountAsync(album.Id)).ReturnsAsync(2);

		var result = await _sut.RemoveSongAsync(album.Id, _userId, songId);

		Assert.True(result.Success);
		_uow.Albums.Verify(a => a.RemoveSongAsync(album.Id, songId), Times.Once);
		_uow.Albums.Verify(a => a.Update(album), Times.Once);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task Delete_NotAuthor_Fails ( )
	{
		var album = Add(ownerId: Guid.NewGuid());

		var result = await _sut.DeleteAsync(album.Id, _userId, false);

		Assert.False(result.Success);
		_uow.Albums.Verify(a => a.Delete(It.IsAny<Album>()), Times.Never);
	}

	[Fact]
	public async Task Delete_CustomCover_RemovesFileFromStorage ( )
	{
		var album = Add(image: "https://cloud/c.png");

		var result = await _sut.DeleteAsync(album.Id, _userId, false);

		Assert.True(result.Success);
		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/c.png"), Times.Once);
		_uow.Albums.Verify(a => a.Delete(album), Times.Once);
	}

	[Fact]
	public async Task Delete_DefaultCover_DoesNotTouchStorage ( )
	{
		var album = Add();

		await _sut.DeleteAsync(album.Id, _userId, false);

		_uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
	}
}
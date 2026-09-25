using BabaTune.Application.DTO.Songs;
using BabaTune.Application.Implementations;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Moq;

namespace BabaTune.Tests.Services;

public class SongServiceTests
{
	private readonly UowMock _uow = new();
	private readonly SongService _sut;
	private readonly Guid _userId = Guid.NewGuid();

	public SongServiceTests ( )
	{
		_sut = new SongService(_uow.Object);
	}

	private Song Add ( Guid? authorId = null, string url = "https://cloud/song.mp3", string image = Defaults.SongImage )
	{
		var song = TestData.NewSong(userId: authorId ?? _userId, url: url, imageUrl: image);
		_uow.Songs.Setup(s => s.GetByIdAsync(song.Id)).ReturnsAsync(song);
		_uow.Songs.Setup(s => s.GetWithAllAsync(song.Id)).ReturnsAsync(song);
		return song;
	}

	private static PagedResult<T> Page<T> ( params T[] items ) => new()
	{
		Items = items.ToList(),
		PageNumber = 1,
		PageSize = 20,
		TotalCount = items.Length
	};

	[Fact]
	public async Task GetById_Missing_ReturnsNull ( )
	{
		var result = await _sut.GetByIdAsync(Guid.NewGuid(), null);

		Assert.Null(result);
	}

	[Fact]
	public async Task GetById_Anonymous_IsNotLikedAndSkipsLikeLookup ( )
	{
		var song = TestData.NewSong();
		_uow.Songs.Setup(s => s.GetWithAllAsync(song.Id)).ReturnsAsync(song);

		var result = await _sut.GetByIdAsync(song.Id, null);

		Assert.NotNull(result);
		Assert.False(result.IsLiked);
		_uow.Playlists.Verify(p => p.GetLikedSongIdsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
	}

	[Fact]
	public async Task GetById_LikedByCurrentUser_IsLiked ( )
	{
		var song = TestData.NewSong();
		_uow.Songs.Setup(s => s.GetWithAllAsync(song.Id)).ReturnsAsync(song);
		_uow.Playlists
			.Setup(p => p.GetLikedSongIdsAsync(_userId, It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new HashSet<Guid> { song.Id });

		var result = await _sut.GetByIdAsync(song.Id, _userId);

		Assert.NotNull(result);
		Assert.True(result.IsLiked);
	}

	[Fact]
	public async Task GetByAuthor_MarksOnlyLikedSongs ( )
	{
		var authorId = Guid.NewGuid();
		var liked = TestData.NewSong(userId: authorId);
		var other = TestData.NewSong(userId: authorId);
		_uow.Songs.Setup(s => s.GetByAuthorAsync(authorId, 1, 20)).ReturnsAsync(Page(liked, other));
		_uow.Playlists
			.Setup(p => p.GetLikedSongIdsAsync(_userId, It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new HashSet<Guid> { liked.Id });

		var result = await _sut.GetByAuthorAsync(authorId, 1, 20, _userId);

		Assert.True(result.Items.Single(i => i.Id == liked.Id).IsLiked);
		Assert.False(result.Items.Single(i => i.Id == other.Id).IsLiked);
		Assert.Equal(2, result.TotalCount);
	}

	[Fact]
	public async Task GetByPlaylist_OwnLikedPlaylist_AllSongsLikedWithoutExtraLookup ( )
	{
		var playlist = new Playlist { Id = Guid.NewGuid(), UserId = _userId, Type = PlaylistType.Liked };
		var first = TestData.NewSong();
		var second = TestData.NewSong();
		_uow.Playlists.Setup(p => p.GetByIdAsync(playlist.Id)).ReturnsAsync(playlist);
		_uow.Playlists.Setup(p => p.GetItemsPagedAsync(playlist.Id, 1, 50)).ReturnsAsync(Page(
			new PlaylistItem { Id = Guid.NewGuid(), PlaylistId = playlist.Id, SongId = first.Id, Song = first },
			new PlaylistItem { Id = Guid.NewGuid(), PlaylistId = playlist.Id, SongId = second.Id, Song = second }));

		var result = await _sut.GetByPlaylistAsync(playlist.Id, 1, 50, _userId);

		Assert.Equal(2, result.Items.Count());
		Assert.All(result.Items, i => Assert.True(i.IsLiked));
		_uow.Playlists.Verify(p => p.GetLikedSongIdsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
	}

	[Fact]
	public async Task GetByPlaylist_Anonymous_NothingIsLiked ( )
	{
		var playlist = new Playlist { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Type = PlaylistType.Custom };
		var song = TestData.NewSong();
		_uow.Playlists.Setup(p => p.GetByIdAsync(playlist.Id)).ReturnsAsync(playlist);
		_uow.Playlists.Setup(p => p.GetItemsPagedAsync(playlist.Id, 1, 50)).ReturnsAsync(Page(
			new PlaylistItem { Id = Guid.NewGuid(), PlaylistId = playlist.Id, SongId = song.Id, Song = song }));

		var result = await _sut.GetByPlaylistAsync(playlist.Id, 1, 50, null);

		Assert.All(result.Items, i => Assert.False(i.IsLiked));
		_uow.Playlists.Verify(p => p.GetLikedSongIdsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
	}

	[Fact]
	public async Task GetByPlaylist_ForeignPlaylist_UsesViewerLikes ( )
	{
		var playlist = new Playlist { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Type = PlaylistType.Liked };
		var liked = TestData.NewSong();
		var other = TestData.NewSong();
		_uow.Playlists.Setup(p => p.GetByIdAsync(playlist.Id)).ReturnsAsync(playlist);
		_uow.Playlists.Setup(p => p.GetItemsPagedAsync(playlist.Id, 1, 50)).ReturnsAsync(Page(
			new PlaylistItem { Id = Guid.NewGuid(), PlaylistId = playlist.Id, SongId = liked.Id, Song = liked },
			new PlaylistItem { Id = Guid.NewGuid(), PlaylistId = playlist.Id, SongId = other.Id, Song = other }));
		_uow.Playlists
			.Setup(p => p.GetLikedSongIdsAsync(_userId, It.IsAny<ICollection<Guid>>()))
			.ReturnsAsync(new HashSet<Guid> { liked.Id });

		var result = await _sut.GetByPlaylistAsync(playlist.Id, 1, 50, _userId);

		Assert.True(result.Items.Single(i => i.Id == liked.Id).IsLiked);
		Assert.False(result.Items.Single(i => i.Id == other.Id).IsLiked);
	}

	[Fact]
	public async Task GetTop_KeepsRankingOrderAndSkipsMissingSongs ( )
	{
		var first = TestData.NewSong();
		var second = TestData.NewSong();
		var missingId = Guid.NewGuid();
		ICollection<Song> songs = new List<Song> { first, second };

		_uow.ListenHistories
			.Setup(l => l.GetTopSongIdsAsync(It.IsAny<DateTime>(), 1, 10))
			.ReturnsAsync(new PagedResult<Guid>
			{
				Items = new List<Guid> { second.Id, missingId, first.Id },
				PageNumber = 1,
				PageSize = 10,
				TotalCount = 3
			});
		_uow.Songs.Setup(s => s.GetByIdsAsync(It.IsAny<ICollection<Guid>>())).ReturnsAsync(songs);

		var result = await _sut.GetTopAsync(TopPeriod.Week, 1, 10, null);

		Assert.Equal(new[] { second.Id, first.Id }, result.Items.Select(i => i.Id).ToArray());
		Assert.Equal(3, result.TotalCount);
	}

	[Fact]
	public async Task GetTop_UnknownPeriod_Throws ( )
	{
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(( ) => _sut.GetTopAsync((TopPeriod)99, 1, 10, null));
	}

	[Fact]
	public async Task Create_WithoutImage_UploadsAudioUsesDefaultCoverAndAttachesKnownCategories ( )
	{
		Song? added = null;
		var category = new Category { Id = 1, Name = "Rock" };
		_uow.Categories.Setup(c => c.GetByIdAsync(1)).ReturnsAsync(category);
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "a.mp3", "audio/mpeg", "songs/audio"))
			.ReturnsAsync("https://cloud/a.mp3");
		_uow.Songs.Setup(s => s.AddAsync(It.IsAny<Song>())).Callback<Song>(s => added = s).Returns(Task.CompletedTask);

		var dto = new CreateSongDto
		{
			AudioFile = FormFiles.Create("a.mp3", "audio/mpeg"),
			CategoryIds = [1, 99],
			GenreIds = []
		};

		var result = await _sut.CreateAsync(dto, _userId);

		Assert.True(result.Success);
		Assert.NotNull(added);
		Assert.Equal("https://cloud/a.mp3", added.Url);
		Assert.Equal(Defaults.SongImage, added.ImageUrl);
		Assert.Equal(_userId, added.UserId);
		Assert.Equal(category, Assert.Single(added.Categories));
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task Create_WithImage_UploadsCoverToCoversFolder ( )
	{
		Song? added = null;
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "a.mp3", "audio/mpeg", "songs/audio"))
			.ReturnsAsync("https://cloud/a.mp3");
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "c.png", "image/png", "songs/covers"))
			.ReturnsAsync("https://cloud/c.png");
		_uow.Songs.Setup(s => s.AddAsync(It.IsAny<Song>())).Callback<Song>(s => added = s).Returns(Task.CompletedTask);

		var dto = new CreateSongDto
		{
			AudioFile = FormFiles.Create("a.mp3", "audio/mpeg"),
			ImageFile = FormFiles.Create("c.png", "image/png"),
			CategoryIds = [],
			GenreIds = []
		};

		await _sut.CreateAsync(dto, _userId);

		Assert.NotNull(added);
		Assert.Equal("https://cloud/c.png", added.ImageUrl);
	}

	[Fact]
	public async Task Create_WhenSaveFails_RemovesUploadedAudio ( )
	{
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "a.mp3", "audio/mpeg", "songs/audio"))
			.ReturnsAsync("https://cloud/a.mp3");
		_uow.Mock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());

		var dto = new CreateSongDto
		{
			AudioFile = FormFiles.Create("a.mp3", "audio/mpeg"),
			CategoryIds = [],
			GenreIds = []
		};

		await Assert.ThrowsAsync<InvalidOperationException>(( ) => _sut.CreateAsync(dto, _userId));

		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/a.mp3"), Times.Once);
	}

	[Fact]
	public async Task Create_WhenSaveFails_RemovesAudioAndCover ( )
	{
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "a.mp3", "audio/mpeg", "songs/audio"))
			.ReturnsAsync("https://cloud/a.mp3");
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "c.png", "image/png", "songs/covers"))
			.ReturnsAsync("https://cloud/c.png");
		_uow.Mock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());

		var dto = new CreateSongDto
		{
			AudioFile = FormFiles.Create("a.mp3", "audio/mpeg"),
			ImageFile = FormFiles.Create("c.png", "image/png"),
			CategoryIds = [],
			GenreIds = []
		};

		await Assert.ThrowsAsync<InvalidOperationException>(( ) => _sut.CreateAsync(dto, _userId));

		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/a.mp3"), Times.Once);
		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/c.png"), Times.Once);
	}

	[Fact]
	public async Task Create_WhenCoverUploadFails_RemovesAlreadyUploadedAudio ( )
	{
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "a.mp3", "audio/mpeg", "songs/audio"))
			.ReturnsAsync("https://cloud/a.mp3");
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "c.png", "image/png", "songs/covers"))
			.ThrowsAsync(new IOException());

		var dto = new CreateSongDto
		{
			AudioFile = FormFiles.Create("a.mp3", "audio/mpeg"),
			ImageFile = FormFiles.Create("c.png", "image/png"),
			CategoryIds = [],
			GenreIds = []
		};

		await Assert.ThrowsAsync<IOException>(( ) => _sut.CreateAsync(dto, _userId));

		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/a.mp3"), Times.Once);
		_uow.Songs.Verify(s => s.AddAsync(It.IsAny<Song>()), Times.Never);
	}

	[Fact]
	public async Task Create_WhenCleanupFails_StillThrowsOriginalException ( )
	{
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
			.ReturnsAsync("https://cloud/a.mp3");
		_uow.Storage.Setup(s => s.DeleteAsync(It.IsAny<string>())).ThrowsAsync(new IOException());
		_uow.Mock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());

		var dto = new CreateSongDto
		{
			AudioFile = FormFiles.Create("a.mp3", "audio/mpeg"),
			CategoryIds = [],
			GenreIds = []
		};

		await Assert.ThrowsAsync<InvalidOperationException>(( ) => _sut.CreateAsync(dto, _userId));
	}

	[Fact]
	public async Task UpdateInfo_NotFound_Fails ( )
	{
		var result = await _sut.UpdateInfoAsync(Guid.NewGuid(), _userId, new UpdateSongDto { CategoryIds = [], GenreIds = [] });

		Assert.False(result.Success);
		Assert.Equal("Song not found.", result.Error);
	}

	[Fact]
	public async Task UpdateInfo_NotAuthor_Fails ( )
	{
		var song = Add(authorId: Guid.NewGuid());

		var result = await _sut.UpdateInfoAsync(song.Id, _userId, new UpdateSongDto { Name = "X", CategoryIds = [], GenreIds = [] });

		Assert.False(result.Success);
		Assert.Equal("You are not the author of this song.", result.Error);
		_uow.Songs.Verify(s => s.Update(It.IsAny<Song>()), Times.Never);
	}

	[Fact]
	public async Task UpdateInfo_Author_UpdatesFieldsAndReplacesCategoriesAndGenres ( )
	{
		var song = Add();
		song.Categories.Add(new Category { Id = 1, Name = "Old" });
		song.Genres.Add(new Genre { Id = 1, Name = "Old" });
		var category = new Category { Id = 2, Name = "New" };
		var genre = new Genre { Id = 3, Name = "New" };
		_uow.Categories.Setup(c => c.GetByIdAsync(2)).ReturnsAsync(category);
		_uow.Genres.Setup(g => g.GetByIdAsync(3)).ReturnsAsync(genre);

		var result = await _sut.UpdateInfoAsync(song.Id, _userId, new UpdateSongDto
		{
			Name = "New name",
			Description = "New description",
			CategoryIds = [2],
			GenreIds = [3]
		});

		Assert.True(result.Success);
		Assert.Equal("New name", song.Name);
		Assert.Equal("New description", song.Description);
		Assert.Equal(category, Assert.Single(song.Categories));
		Assert.Equal(genre, Assert.Single(song.Genres));
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task UpdateInfo_EmptyIds_ClearsCategoriesAndGenres ( )
	{
		var song = Add();
		song.Categories.Add(new Category { Id = 1, Name = "Old" });
		song.Genres.Add(new Genre { Id = 1, Name = "Old" });

		var result = await _sut.UpdateInfoAsync(song.Id, _userId, new UpdateSongDto { Name = "N", CategoryIds = [], GenreIds = [] });

		Assert.True(result.Success);
		Assert.Empty(song.Categories);
		Assert.Empty(song.Genres);
	}

	[Fact]
	public async Task UpdateImage_NotAuthor_Fails ( )
	{
		var song = Add(authorId: Guid.NewGuid());

		var result = await _sut.UpdateImageAsync(song.Id, _userId, null);

		Assert.False(result.Success);
		_uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task UpdateImage_NewFile_ReplacesOldCover ( )
	{
		var song = Add(image: "https://cloud/old.png");
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "new.png", "image/png", "songs/covers"))
			.ReturnsAsync("https://cloud/new.png");

		var result = await _sut.UpdateImageAsync(song.Id, _userId, FormFiles.Create("new.png", "image/png"));

		Assert.True(result.Success);
		Assert.Equal("https://cloud/new.png", song.ImageUrl);
		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/old.png"), Times.Once);
	}

	[Fact]
	public async Task UpdateImage_Reset_KeepsDefaultCoverOutOfStorageDeletion ( )
	{
		var song = Add();

		var result = await _sut.UpdateImageAsync(song.Id, _userId, null);

		Assert.True(result.Success);
		Assert.Equal(Defaults.SongImage, song.ImageUrl);
		_uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task UpdateAudio_NotAuthor_Fails ( )
	{
		var song = Add(authorId: Guid.NewGuid());

		var result = await _sut.UpdateAudioAsync(song.Id, _userId, FormFiles.Create("a.mp3", "audio/mpeg"));

		Assert.False(result.Success);
		_uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task UpdateAudio_Author_ReplacesFile ( )
	{
		var song = Add(url: "https://cloud/old.mp3");
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "new.mp3", "audio/mpeg", "songs/audio"))
			.ReturnsAsync("https://cloud/new.mp3");

		var result = await _sut.UpdateAudioAsync(song.Id, _userId, FormFiles.Create("new.mp3", "audio/mpeg"));

		Assert.True(result.Success);
		Assert.Equal("https://cloud/new.mp3", song.Url);
		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/old.mp3"), Times.Once);
	}

	[Fact]
	public async Task Delete_NotAuthor_Fails ( )
	{
		var song = Add(authorId: Guid.NewGuid());

		var result = await _sut.DeleteAsync(song.Id, _userId);

		Assert.False(result.Success);
		Assert.Equal("You are not the author of this song.", result.Error);
		_uow.Songs.Verify(s => s.Delete(It.IsAny<Song>()), Times.Never);
		_uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task Delete_CustomCover_RemovesAudioAndCover ( )
	{
		var song = Add(url: "https://cloud/a.mp3", image: "https://cloud/c.png");

		var result = await _sut.DeleteAsync(song.Id, _userId);

		Assert.True(result.Success);
		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/a.mp3"), Times.Once);
		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/c.png"), Times.Once);
		_uow.Songs.Verify(s => s.Delete(song), Times.Once);
	}

	[Fact]
	public async Task Delete_DefaultCover_RemovesOnlyAudio ( )
	{
		var song = Add(url: "https://cloud/a.mp3");

		await _sut.DeleteAsync(song.Id, _userId);

		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/a.mp3"), Times.Once);
		_uow.Storage.Verify(s => s.DeleteAsync(Defaults.SongImage), Times.Never);
	}

	[Fact]
	public async Task UpdateImage_WhenSaveFails_RemovesNewFileAndKeepsOld ( )
	{
		var song = Add(image: "https://cloud/old.png");
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "new.png", "image/png", "songs/covers"))
			.ReturnsAsync("https://cloud/new.png");
		_uow.Mock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());

		await Assert.ThrowsAsync<InvalidOperationException>(( ) =>
			_sut.UpdateImageAsync(song.Id, _userId, FormFiles.Create("new.png", "image/png")));

		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/new.png"), Times.Once);
		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/old.png"), Times.Never);
	}

	[Fact]
	public async Task UpdateAudio_WhenSaveFails_RemovesNewFileAndKeepsOld ( )
	{
		var song = Add(url: "https://cloud/old.mp3");
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "new.mp3", "audio/mpeg", "songs/audio"))
			.ReturnsAsync("https://cloud/new.mp3");
		_uow.Mock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());

		await Assert.ThrowsAsync<InvalidOperationException>(( ) =>
			_sut.UpdateAudioAsync(song.Id, _userId, FormFiles.Create("new.mp3", "audio/mpeg")));

		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/new.mp3"), Times.Once);
		_uow.Storage.Verify(s => s.DeleteAsync("https://cloud/old.mp3"), Times.Never);
	}

	[Fact]
	public async Task UpdateAudio_WhenUploadFails_KeepsOldFile ( )
	{
		var song = Add(url: "https://cloud/old.mp3");
		_uow.Storage
			.Setup(s => s.UploadAsync(It.IsAny<Stream>(), "new.mp3", "audio/mpeg", "songs/audio"))
			.ThrowsAsync(new IOException());

		await Assert.ThrowsAsync<IOException>(( ) =>
			_sut.UpdateAudioAsync(song.Id, _userId, FormFiles.Create("new.mp3", "audio/mpeg")));

		_uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
	}

	[Fact]
	public async Task Delete_WhenSaveFails_KeepsFilesInStorage ( )
	{
		var song = Add(url: "https://cloud/a.mp3", image: "https://cloud/c.png");
		_uow.Mock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());

		await Assert.ThrowsAsync<InvalidOperationException>(( ) => _sut.DeleteAsync(song.Id, _userId));

		_uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task Delete_WhenStorageCleanupFailsAfterSave_StillSucceeds ( )
	{
		var song = Add(url: "https://cloud/a.mp3");
		_uow.Storage.Setup(s => s.DeleteAsync(It.IsAny<string>())).ThrowsAsync(new IOException());

		var result = await _sut.DeleteAsync(song.Id, _userId);

		Assert.True(result.Success);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}
}
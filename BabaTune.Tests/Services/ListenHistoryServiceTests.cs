using BabaTune.Application.DTO.ListenHistory;
using BabaTune.Application.Implementations;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Moq;

namespace BabaTune.Tests.Services;

public class ListenHistoryServiceTests
{
    private readonly UowMock _uow = new();
    private readonly ListenHistoryService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public ListenHistoryServiceTests()
    {
        _sut = new ListenHistoryService(_uow.Object);
    }

    [Fact]
    public async Task Record_SongMissing_Fails()
    {
        var result = await _sut.RecordAsync(new RecordListenHistoryDto { UserId = _userId, SongId = Guid.NewGuid(), PlayedPercent = 50 });

        Assert.False(result.Success);
        Assert.Equal("Song not found.", result.Error);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Record_FirstListen_CreatesEntryWithCurrentLikeState(bool isLiked)
    {
        var song = TestData.NewSong();
        ListenHistory? added = null;
        _uow.Songs.Setup(s => s.GetByIdAsync(song.Id)).ReturnsAsync(song);
        _uow.Playlists
            .Setup(p => p.GetLikedSongIdsAsync(_userId, It.IsAny<ICollection<Guid>>()))
            .ReturnsAsync(isLiked ? new HashSet<Guid> { song.Id } : new HashSet<Guid>());
        _uow.ListenHistories
            .Setup(h => h.AddAsync(It.IsAny<ListenHistory>()))
            .Callback<ListenHistory>(h => added = h)
            .Returns(Task.CompletedTask);

        var result = await _sut.RecordAsync(new RecordListenHistoryDto { UserId = _userId, SongId = song.Id, PlayedPercent = 40 });

        Assert.True(result.Success);
        Assert.NotNull(added);
        Assert.Equal(song.Id, added.SongId);
        Assert.Equal(_userId, added.UserId);
        Assert.Equal(isLiked, added.IsLiked);
        Assert.Equal(40, added.PlayedPercent);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(80, 30, 80)]
    [InlineData(30, 80, 80)]
    [InlineData(50, 50, 50)]
    public async Task Record_RepeatListen_KeepsMaxPercentAndRefreshesTimestamp(int existing, int incoming, int expected)
    {
        var song = TestData.NewSong();
        var previous = DateTime.UtcNow.AddDays(-1);
        var entry = new ListenHistory
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SongId = song.Id,
            PlayedPercent = existing,
            ListenedAt = previous
        };
        _uow.Songs.Setup(s => s.GetByIdAsync(song.Id)).ReturnsAsync(song);
        _uow.ListenHistories.Setup(h => h.GetEntryAsync(_userId, song.Id)).ReturnsAsync(entry);

        var result = await _sut.RecordAsync(new RecordListenHistoryDto { UserId = _userId, SongId = song.Id, PlayedPercent = incoming });

        Assert.True(result.Success);
        Assert.Equal(expected, entry.PlayedPercent);
        Assert.True(entry.ListenedAt > previous);
        _uow.ListenHistories.Verify(h => h.AddAsync(It.IsAny<ListenHistory>()), Times.Never);
    }

    [Fact]
    public async Task SyncLike_NoEntry_DoesNothing()
    {
        await _sut.SyncLikeAsync(_userId, Guid.NewGuid(), true);

        _uow.ListenHistories.Verify(h => h.Update(It.IsAny<ListenHistory>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SyncLike_ExistingEntry_UpdatesFlag(bool isLiked)
    {
        var songId = Guid.NewGuid();
        var entry = new ListenHistory { Id = Guid.NewGuid(), UserId = _userId, SongId = songId, IsLiked = !isLiked };
        _uow.ListenHistories.Setup(h => h.GetEntryAsync(_userId, songId)).ReturnsAsync(entry);

        await _sut.SyncLikeAsync(_userId, songId, isLiked);

        Assert.Equal(isLiked, entry.IsLiked);
        _uow.ListenHistories.Verify(h => h.Update(entry), Times.Once);
    }

    [Fact]
    public async Task GetHistory_Empty_SkipsLikeLookup()
    {
        _uow.ListenHistories
            .Setup(h => h.GetByUserAsync(_userId, 1, 20))
            .ReturnsAsync(new PagedResult<ListenHistory> { Items = new List<ListenHistory>(), PageNumber = 1, PageSize = 20, TotalCount = 0 });

        var result = await _sut.GetHistoryAsync(_userId, 1, 20);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        _uow.Playlists.Verify(p => p.GetLikedSongIdsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task GetHistory_MarksLikedSongs()
    {
        var liked = TestData.NewSong();
        var other = TestData.NewSong();
        _uow.ListenHistories
            .Setup(h => h.GetByUserAsync(_userId, 1, 20))
            .ReturnsAsync(new PagedResult<ListenHistory>
            {
                Items = new List<ListenHistory>
                {
                    new() { Id = Guid.NewGuid(), UserId = _userId, SongId = liked.Id, Song = liked },
                    new() { Id = Guid.NewGuid(), UserId = _userId, SongId = other.Id, Song = other }
                },
                PageNumber = 1,
                PageSize = 20,
                TotalCount = 2
            });
        _uow.Playlists
            .Setup(p => p.GetLikedSongIdsAsync(_userId, It.IsAny<ICollection<Guid>>()))
            .ReturnsAsync(new HashSet<Guid> { liked.Id });

        var result = await _sut.GetHistoryAsync(_userId, 1, 20);

        Assert.True(result.Items.Single(i => i.Id == liked.Id).IsLiked);
        Assert.False(result.Items.Single(i => i.Id == other.Id).IsLiked);
        Assert.Equal(2, result.TotalCount);
    }
}

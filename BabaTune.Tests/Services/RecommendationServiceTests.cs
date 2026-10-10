using BabaTune.Application.DTO.Songs;
using BabaTune.Application.Implementations;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace BabaTune.Tests.Services
{
	public class RecommendationServiceTests
	{
		private readonly UowMock _uow = new();
		private readonly RecommendationService _sut;
		private readonly Guid _userId = Guid.NewGuid();

		private readonly List<Song> _songs = new();
		private readonly List<Guid> _popular = new();
		private readonly List<Song> _candidates = new();
		private readonly List<Song> _subCandidates = new();
		private readonly List<ListenSignal> _signals = new();
		private readonly HashSet<Guid> _listened = new();
		private readonly HashSet<Guid> _subscribed = new();
		private readonly HashSet<Guid> _liked = new();
		private readonly Dictionary<Guid, int> _counts = new();

		public RecommendationServiceTests ( )
		{
			_uow.ListenHistories
				.Setup(h => h.GetRecentSongIdsAsync(It.IsAny<Guid>(), It.IsAny<int>()))
				.ReturnsAsync(( ) => _listened);
			_uow.ListenHistories
				.Setup(h => h.GetSignalsAsync(It.IsAny<Guid>(), It.IsAny<int>()))
				.ReturnsAsync(( ) => (ICollection<ListenSignal>)_signals.ToList());
			_uow.ListenHistories
				.Setup(h => h.GetListenCountsAsync(It.IsAny<ICollection<Guid>>(), It.IsAny<DateTime>()))
				.ReturnsAsync(( ) => _counts);
			_uow.ListenHistories
				.Setup(h => h.GetTopSongIdsAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>()))
				.ReturnsAsync(( ) => new PagedResult<Guid>
				{
					Items = _popular.ToList(),
					PageNumber = 1,
					PageSize = 300,
					TotalCount = _popular.Count
				});

			_uow.Songs
				.Setup(s => s.GetByIdsAsync(It.IsAny<ICollection<Guid>>()))
				.ReturnsAsync(( ICollection<Guid> ids ) => (ICollection<Song>)_songs.Where(s => ids.Contains(s.Id)).ToList());
			_uow.Songs
				.Setup(s => s.GetCandidatesAsync(It.IsAny<Guid>(), It.IsAny<ICollection<int>>(), It.IsAny<ICollection<int>>(), It.IsAny<ICollection<Guid>>(), It.IsAny<int>()))
				.ReturnsAsync(( ) => (ICollection<Song>)_candidates.ToList());
			_uow.Songs
				.Setup(s => s.GetSubscriptionCandidatesAsync(It.IsAny<ICollection<Guid>>(), It.IsAny<ICollection<Guid>>(), It.IsAny<int>()))
				.ReturnsAsync(( ) => (ICollection<Song>)_subCandidates.ToList());

			_uow.Subscribes
				.Setup(s => s.GetSubscribedToIdsAsync(It.IsAny<Guid>()))
				.ReturnsAsync(( ) => _subscribed);
			_uow.Playlists
				.Setup(p => p.GetLikedSongIdsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()))
				.ReturnsAsync(( ) => _liked);

			_sut = new RecommendationService(_uow.Object, new MemoryCache(new MemoryCacheOptions()));
		}

		private Song Add ( Guid? author = null, DateTime? created = null, params int[] genres )
		{
			var song = TestData.NewSong(userId: author ?? Guid.NewGuid());
			song.CreatedAt = created ?? DateTime.UtcNow.AddDays(-30);
			foreach(var g in genres)
				song.Genres.Add(new Genre { Id = g, Name = "G" });
			_songs.Add(song);
			return song;
		}

		private List<Song> AddPopular ( int count )
		{
			var list = Enumerable.Range(0, count).Select(_ => Add()).ToList();
			_popular.AddRange(list.Select(s => s.Id));
			return list;
		}

		private static List<Guid> Ids ( FeedPageDto page ) => page.Items.Select(i => i.Id).ToList();

		[Fact]
		public async Task GetFeed_NoHistoryAndNoPopular_ReturnsEmpty ( )
		{
			var result = await _sut.GetFeedAsync(_userId, 20);

			Assert.Empty(result.Items);
		}

		[Fact]
		public async Task GetFeed_NewUser_ReturnsPopularWithoutOwnSongs ( )
		{
			var others = AddPopular(5);
			var own = Add(author: _userId);
			_popular.Add(own.Id);

			var result = await _sut.GetFeedAsync(_userId, 10);

			var ids = Ids(result);
			Assert.DoesNotContain(own.Id, ids);
			Assert.Equal(others.Select(s => s.Id).OrderBy(x => x), ids.OrderBy(x => x));
		}

		[Fact]
		public async Task GetFeed_ListenedSongs_AreNeverReturned ( )
		{
			var popular = AddPopular(6);
			_listened.Add(popular[0].Id);
			_listened.Add(popular[1].Id);

			var result = await _sut.GetFeedAsync(_userId, 10);

			var ids = Ids(result);
			Assert.DoesNotContain(popular[0].Id, ids);
			Assert.DoesNotContain(popular[1].Id, ids);
			Assert.Equal(4, ids.Count);
		}

		[Fact]
		public async Task GetFeed_ResultNeverExceedsTakeAndHasNoDuplicates ( )
		{
			AddPopular(50);

			var result = await _sut.GetFeedAsync(_userId, 20);

			var ids = Ids(result);
			Assert.Equal(20, ids.Count);
			Assert.Equal(ids.Count, ids.Distinct().Count());
		}

		[Fact]
		public async Task GetFeed_SecondCallWithinTtl_UsesCachedPoolAndReturnsDifferentSongs ( )
		{
			AddPopular(10);

			var first = Ids(await _sut.GetFeedAsync(_userId, 4));
			var second = Ids(await _sut.GetFeedAsync(_userId, 4));

			Assert.Equal(4, first.Count);
			Assert.Equal(4, second.Count);
			Assert.Empty(first.Intersect(second));
			_uow.ListenHistories.Verify(h => h.GetSignalsAsync(_userId, It.IsAny<int>()), Times.Once);
		}

		[Fact]
		public async Task GetFeed_PoolExhausted_RebuildsAndFillsWithoutDuplicates ( )
		{
			AddPopular(6);

			await _sut.GetFeedAsync(_userId, 4);
			var second = Ids(await _sut.GetFeedAsync(_userId, 4));

			Assert.Equal(4, second.Count);
			Assert.Equal(4, second.Distinct().Count());
			_uow.ListenHistories.Verify(h => h.GetSignalsAsync(_userId, It.IsAny<int>()), Times.Exactly(2));
		}

		[Fact]
		public async Task GetFeed_NotEnoughSongsInCatalog_ReturnsWhatExists ( )
		{
			AddPopular(3);

			var result = await _sut.GetFeedAsync(_userId, 10);

			Assert.Equal(3, result.Items.Count);
		}

		[Fact]
		public async Task GetFeed_SameAuthor_AtMostTwoPerPage ( )
		{
			var authorA = Guid.NewGuid();
			var a = Enumerable.Range(0, 5).Select(_ => Add(author: authorA)).ToList();
			var b = Add();
			_popular.AddRange(a.Select(s => s.Id));
			_popular.Add(b.Id);

			var ids = Ids(await _sut.GetFeedAsync(_userId, 3));

			Assert.Equal(3, ids.Count);
			Assert.Equal(2, ids.Count(id => a.Any(s => s.Id == id)));
			Assert.Contains(b.Id, ids);
		}

		[Fact]
		public async Task GetFeed_IsLiked_ComesFromLikedPlaylist ( )
		{
			var popular = AddPopular(4);
			_liked.Add(popular[0].Id);

			var result = await _sut.GetFeedAsync(_userId, 10);

			Assert.True(result.Items.Single(i => i.Id == popular[0].Id).IsLiked);
			Assert.All(result.Items.Where(i => i.Id != popular[0].Id), i => Assert.False(i.IsLiked));
		}

		[Fact]
		public async Task GetFeed_WithHistory_RanksCandidatesByProfileAndDropsNonMatching ( )
		{
			_signals.Add(new ListenSignal
			{
				SongId = Guid.NewGuid(),
				ListenedAt = DateTime.UtcNow,
				PlayedPercent = 100,
				IsLiked = true,
				GenreIds = [1]
			});
			var match = Add(created: DateTime.UtcNow, genres: 1);
			var other = Add(created: DateTime.UtcNow, genres: 99);
			_candidates.Add(match);
			_candidates.Add(other);

			var ids = Ids(await _sut.GetFeedAsync(_userId, 1));

			Assert.Contains(match.Id, ids);
			Assert.DoesNotContain(other.Id, ids);
			_uow.Songs.Verify(s => s.GetCandidatesAsync(
				_userId,
				It.Is<ICollection<int>>(g => g.Contains(1)),
				It.IsAny<ICollection<int>>(),
				It.Is<ICollection<Guid>>(e => e.Contains(_signals[0].SongId)),
				It.IsAny<int>()), Times.Once);
		}

		[Fact]
		public async Task GetFeed_TakeBelowFive_SkipsSubscriptions ( )
		{
			AddPopular(10);
			_subscribed.Add(Guid.NewGuid());

			await _sut.GetFeedAsync(_userId, 4);

			_uow.Subscribes.Verify(s => s.GetSubscribedToIdsAsync(It.IsAny<Guid>()), Times.Never);
		}

		[Fact]
		public async Task GetFeed_NoSubscriptions_DoesNotQuerySubscriptionSongs ( )
		{
			AddPopular(30);

			var result = await _sut.GetFeedAsync(_userId, 20);

			Assert.Equal(20, result.Items.Count);
			_uow.Songs.Verify(s => s.GetSubscriptionCandidatesAsync(It.IsAny<ICollection<Guid>>(), It.IsAny<ICollection<Guid>>(), It.IsAny<int>()), Times.Never);
		}

		[Fact]
		public async Task GetFeed_FreshSubscriptionSongs_AreEvenlyInsertedNewestFirst ( )
		{
			AddPopular(30);
			_subscribed.Add(Guid.NewGuid());
			var now = DateTime.UtcNow;
			var subs = Enumerable.Range(0, 4)
				.Select(i => Add(created: now.AddHours(-i - 1)))
				.ToList();
			_subCandidates.AddRange(subs.OrderBy(_ => Guid.NewGuid()));

			var ids = Ids(await _sut.GetFeedAsync(_userId, 20));

			Assert.Equal(20, ids.Count);
			Assert.Equal(subs[0].Id, ids[4]);
			Assert.Equal(subs[1].Id, ids[8]);
			Assert.Equal(subs[2].Id, ids[12]);
			Assert.Equal(subs[3].Id, ids[16]);
			Assert.Equal(20, ids.Distinct().Count());
		}

		[Fact]
		public async Task GetFeed_OldSubscriptionSongs_FillRemainingSlotsByPopularity ( )
		{
			AddPopular(30);
			_subscribed.Add(Guid.NewGuid());
			var old = Enumerable.Range(0, 4).Select(i => Add(created: DateTime.UtcNow.AddDays(-20 - i))).ToList();
			_subCandidates.AddRange(old);
			foreach(var s in old)
				_counts[s.Id] = 10;

			var ids = Ids(await _sut.GetFeedAsync(_userId, 20));

			Assert.All(old, s => Assert.Contains(s.Id, ids));
		}

		[Fact]
		public async Task GetFeed_SubscriptionSongsPerAuthor_LimitedToThree ( )
		{
			AddPopular(30);
			_subscribed.Add(Guid.NewGuid());
			var author = Guid.NewGuid();
			var songs = Enumerable.Range(0, 5).Select(i => Add(author: author, created: DateTime.UtcNow.AddHours(-i - 1))).ToList();
			_subCandidates.AddRange(songs);

			var ids = Ids(await _sut.GetFeedAsync(_userId, 20));

			Assert.Equal(3, ids.Count(id => songs.Any(s => s.Id == id)));
		}

		[Fact]
		public async Task GetFeed_SubscriptionSongs_AreNotRepeatedOnNextPage ( )
		{
			AddPopular(60);
			_subscribed.Add(Guid.NewGuid());
			var subs = Enumerable.Range(0, 4).Select(i => Add(created: DateTime.UtcNow.AddHours(-i - 1))).ToList();
			_subCandidates.AddRange(subs);

			await _sut.GetFeedAsync(_userId, 20);
			await _sut.GetFeedAsync(_userId, 20);

			_uow.Songs.Verify(s => s.GetSubscriptionCandidatesAsync(
				It.IsAny<ICollection<Guid>>(),
				It.Is<ICollection<Guid>>(e => subs.All(x => e.Contains(x.Id))),
				It.IsAny<int>()), Times.Once);
		}

		[Fact]
		public async Task GetFeed_ListenedSongs_AreExcludedFromSubscriptionQuery ( )
		{
			AddPopular(30);
			_subscribed.Add(Guid.NewGuid());
			var listenedId = Guid.NewGuid();
			_listened.Add(listenedId);

			await _sut.GetFeedAsync(_userId, 20);

			_uow.Songs.Verify(s => s.GetSubscriptionCandidatesAsync(
				It.IsAny<ICollection<Guid>>(),
				It.Is<ICollection<Guid>>(e => e.Contains(listenedId)),
				It.IsAny<int>()), Times.Once);
		}

		[Fact]
		public async Task GetFeed_SubscriptionSongsAreNotDuplicatedByPool ( )
		{
			var popular = AddPopular(30);
			_subscribed.Add(Guid.NewGuid());
			var shared = popular[0];
			shared.CreatedAt = DateTime.UtcNow.AddHours(-1);
			_subCandidates.Add(shared);

			var ids = Ids(await _sut.GetFeedAsync(_userId, 20));

			Assert.Equal(ids.Count, ids.Distinct().Count());
		}

		[Fact]
		public async Task GetFeed_ParallelCallsForSameUser_BuildPoolOnce ( )
		{
			AddPopular(100);

			var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => _sut.GetFeedAsync(_userId, 4)));

			Assert.All(results, r => Assert.Equal(4, r.Items.Count));
			_uow.ListenHistories.Verify(h => h.GetSignalsAsync(_userId, It.IsAny<int>()), Times.Once);
		}

		[Fact]
		public async Task GetFeed_PopularListIsSharedBetweenUsers ( )
		{
			AddPopular(20);

			await _sut.GetFeedAsync(Guid.NewGuid(), 4);
			await _sut.GetFeedAsync(Guid.NewGuid(), 4);

			_uow.ListenHistories.Verify(h => h.GetTopSongIdsAsync(It.IsAny<DateTime>(), 1, It.IsAny<int>()), Times.Once);
		}
	}
}

using BabaTune.Application.Common;
using BabaTune.Application.DTO.Songs;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using static BabaTune.Application.Common.Constants.Recommendations;

namespace BabaTune.Application.Implementations
{
	public class RecommendationService : IRecommendationService
	{
		private static readonly SemaphoreSlim[] Gates =
			Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

		private sealed class PoolEntry
		{
			public PoolEntry ( List<ScoredSong> items ) => Items = items;
			public List<ScoredSong> Items { get; }
			public object Lock { get; } = new();
		}

		private readonly IUnitOfWork _uow;
		private readonly IMemoryCache _cache;

		public RecommendationService ( IUnitOfWork uow, IMemoryCache cache )
		{
			_uow = uow;
			_cache = cache;
		}

		public async Task<FeedPageDto> GetFeedAsync ( Guid userId, int take )
		{
			var listened = await _uow.ListenHistories.GetRecentSongIdsAsync(userId, HistoryTake);

			var subscriptionIds = await PickSubscriptionsAsync(userId, take * SubscriptionPercent / 100, listened);

			var skip = new HashSet<Guid>(listened);
			skip.UnionWith(subscriptionIds);

			var need = take - subscriptionIds.Count;
			var picked = new List<Guid>();

			var pool = await GetPoolAsync(userId, listened, null);
			picked.AddRange(PickFromPool(pool, skip, need));

			if(picked.Count < need)
			{
				skip.UnionWith(picked);
				pool = await GetPoolAsync(userId, skip, pool);
				picked.AddRange(PickFromPool(pool, skip, need - picked.Count));
			}

			if(picked.Count < need)
			{
				skip.UnionWith(picked);
				picked.AddRange(await PopularAsync(userId, skip, need - picked.Count));
			}

			var step = Math.Max(1, take / (subscriptionIds.Count + 1));
			for(var i = 0; i < subscriptionIds.Count; i++)
				picked.Insert(Math.Min(picked.Count, (i + 1) * step), subscriptionIds[i]);

			return new FeedPageDto { Items = await LoadDtosAsync(picked, userId) };
		}

		private async Task<List<Guid>> GetPopularIdsAsync ( )
		{
			return (await _cache.GetOrCreateAsync("recs:popular", async e =>
			{
				e.AbsoluteExpirationRelativeToNow = PopularTtl;
				var top = await _uow.ListenHistories.GetTopSongIdsAsync(DateTime.UtcNow.AddMonths(-2), 1, PopularListSize);
				return top.Items.ToList();
			}))!;
		}

		private async Task<PoolEntry> GetPoolAsync ( Guid userId, HashSet<Guid> exclude, PoolEntry? stale )
		{
			var key = $"recs:pool:{userId}";

			if(_cache.TryGetValue(key, out PoolEntry? cached) && cached is not null && !ReferenceEquals(cached, stale))
				return cached;

			var gate = Gates[(userId.GetHashCode() & int.MaxValue) % Gates.Length];
			await gate.WaitAsync();
			try
			{
				if(_cache.TryGetValue(key, out cached) && cached is not null && !ReferenceEquals(cached, stale))
					return cached;

				var built = new PoolEntry(await BuildPoolAsync(userId, exclude));

				if(built.Items.Count > 0)
					_cache.Set(key, built, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = PoolTtl });

				return built;
			}
			finally
			{
				gate.Release();
			}
		}

		private async Task<List<ScoredSong>> BuildPoolAsync ( Guid userId, HashSet<Guid> exclude )
		{
			var now = DateTime.UtcNow;
			var pool = new List<ScoredSong>();

			var signals = await _uow.ListenHistories.GetSignalsAsync(userId, HistoryTake);
			if(signals.Count > 0)
			{
				var profile = ContentBasedScorer.BuildProfile(signals.ToList(), now);
				var genreIds = ContentBasedScorer.TopIds(profile.Genres, TopTagsCount);
				var categoryIds = ContentBasedScorer.TopIds(profile.Categories, TopTagsCount);

				if(genreIds.Count > 0 || categoryIds.Count > 0)
				{
					var excluded = new HashSet<Guid>(exclude);
					excluded.UnionWith(signals.Select(s => s.SongId));

					var candidates = await _uow.Songs.GetCandidatesAsync(
						userId, genreIds, categoryIds, excluded, CandidatesTake);

					var counts = candidates.Count == 0
						? new Dictionary<Guid, int>()
						: await _uow.ListenHistories.GetListenCountsAsync(
							candidates.Select(s => s.Id).ToList(), now.AddMonths(-2));

					var maxLog = counts.Count == 0 ? 0 : Math.Log10(counts.Values.Max() + 1);
					double Popularity ( Guid id )
						=> maxLog == 0 ? 0 : Math.Log10(counts.GetValueOrDefault(id) + 1) / maxLog;

					var perAuthor = new Dictionary<Guid, int>();
					var scored = candidates
						.Select(s => new ScoredSong(
							s.Id,
							s.UserId ?? Guid.Empty,
							ContentBasedScorer.Score(s, profile, now) * (1 + PopularityWeight * Popularity(s.Id))))
						.Where(x => x.Score > 0)
						.OrderByDescending(x => x.Score);

					foreach(var song in scored)
					{
						var used = perAuthor.GetValueOrDefault(song.AuthorId);
						if(used >= MaxPerAuthorInPool)
							continue;

						perAuthor[song.AuthorId] = used + 1;
						pool.Add(song);

						if(pool.Count == PoolSize - ExplorationReserve)
							break;
					}
				}
			}

			var explorationNeed = PoolSize - pool.Count;
			if(explorationNeed > 0)
			{
				var known = pool.Select(x => x.SongId).ToHashSet();
				var extra = (await GetPopularIdsAsync())
					.Where(x => !known.Contains(x) && !exclude.Contains(x))
					.OrderBy(_ => Random.Shared.Next())
					.Take(explorationNeed * 2)
					.ToList();

				if(extra.Count > 0)
				{
					var songs = (await _uow.Songs.GetByIdsAsync(extra))
						.Where(s => s.UserId != userId)
						.Take(explorationNeed);

					pool.AddRange(songs.Select(s => new ScoredSong(s.Id, s.UserId ?? Guid.Empty, ExplorationScore)));
				}
			}

			return pool;
		}

		private static List<Guid> PickFromPool ( PoolEntry entry, HashSet<Guid> skip, int take )
		{
			var picked = new List<Guid>();
			if(take <= 0)
				return picked;

			lock(entry.Lock)
			{
				entry.Items.RemoveAll(s => skip.Contains(s.SongId));

				var perAuthor = new Dictionary<Guid, int>();

				var ordered = entry.Items
					.Select(s => (Song: s, Key: Math.Pow(Random.Shared.NextDouble(), 1.0 / Math.Exp(3 * s.Score))))
					.OrderByDescending(x => x.Key);

				foreach(var (song, _) in ordered)
				{
					var used = perAuthor.GetValueOrDefault(song.AuthorId);
					if(used >= MaxPerAuthorPerPage)
						continue;

					perAuthor[song.AuthorId] = used + 1;
					picked.Add(song.SongId);

					if(picked.Count == take)
						break;
				}

				var pickedSet = picked.ToHashSet();
				entry.Items.RemoveAll(s => pickedSet.Contains(s.SongId));
			}

			return picked;
		}

		private HashSet<Guid> SeenSubscriptions ( Guid userId )
			=> _cache.GetOrCreate($"recs:subs:{userId}", e =>
			{
				e.SlidingExpiration = SeenSubscriptionsTtl;
				return new HashSet<Guid>();
			})!;

		private async Task<List<Guid>> PickSubscriptionsAsync ( Guid userId, int count, HashSet<Guid> listened )
		{
			if(count <= 0)
				return [];

			var authorIds = await _uow.Subscribes.GetSubscribedToIdsAsync(userId);
			if(authorIds.Count == 0)
				return [];

			var seen = SeenSubscriptions(userId);
			var exclude = new HashSet<Guid>(listened);
			lock(seen)
				exclude.UnionWith(seen);

			var raw = await _uow.Songs.GetSubscriptionCandidatesAsync(authorIds, exclude, SubscriptionCandidates);

			var perAuthor = new Dictionary<Guid, int>();
			var candidates = new List<Song>();
			foreach(var song in raw.OrderByDescending(s => s.CreatedAt))
			{
				var author = song.UserId ?? Guid.Empty;
				var used = perAuthor.GetValueOrDefault(author);
				if(used >= SubscriptionPerAuthor)
					continue;

				perAuthor[author] = used + 1;
				candidates.Add(song);
			}

			if(candidates.Count == 0)
				return [];

			var now = DateTime.UtcNow;
			var freshBorder = now.AddHours(-SubscriptionFreshHours);

			var result = candidates
				.Where(s => s.CreatedAt >= freshBorder)
				.Take(count)
				.ToList();

			var left = count - result.Count;
			if(left > 0)
			{
				var taken = result.Select(s => s.Id).ToHashSet();
				var rest = candidates.Where(s => !taken.Contains(s.Id)).ToList();

				if(rest.Count > 0)
				{
					var counts = await _uow.ListenHistories.GetListenCountsAsync(
						rest.Select(s => s.Id).ToList(), now.AddMonths(-2));

					var popular = rest
						.OrderByDescending(s => counts.GetValueOrDefault(s.Id))
						.ThenByDescending(s => s.CreatedAt)
						.Take(SubscriptionShortlist)
						.OrderBy(_ => Random.Shared.Next())
						.Take(left)
						.ToList();

					result.AddRange(popular);
				}
			}

			var ids = result.Select(s => s.Id).ToList();
			lock(seen)
				seen.UnionWith(ids);

			return ids;
		}

		private async Task<List<Guid>> PopularAsync ( Guid userId, HashSet<Guid> skip, int take )
		{
			var ids = (await GetPopularIdsAsync())
				.Where(id => !skip.Contains(id))
				.Take(take * 3)
				.ToList();

			if(ids.Count == 0)
				return [];

			var own = (await _uow.Songs.GetByIdsAsync(ids))
				.Where(s => s.UserId == userId)
				.Select(s => s.Id)
				.ToHashSet();

			var result = ids.Where(id => !own.Contains(id)).Take(take).ToList();
			skip.UnionWith(result);
			return result;
		}

		private async Task<List<SongDto>> LoadDtosAsync ( List<Guid> ids, Guid userId )
		{
			if(ids.Count == 0)
				return [];

			var songs = (await _uow.Songs.GetByIdsAsync(ids)).ToDictionary(s => s.Id);
			var liked = await _uow.Playlists.GetLikedSongIdsAsync(userId, ids);

			return ids
				.Where(songs.ContainsKey)
				.Select(id => SongMapper.ToDto(songs[id], liked.Contains(id)))
				.ToList();
		}
	}
}
using BabaTune.Application.Common;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Tests.Services
{
	public class ContentBasedScorerTests
	{
		private static readonly DateTime Now = new(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);

		private static ListenSignal Signal ( int[] genres, double percent = 100, bool liked = false, double ageDays = 0, int[]? categories = null ) => new()
		{
			SongId = Guid.NewGuid(),
			ListenedAt = Now.AddDays(-ageDays),
			PlayedPercent = percent,
			IsLiked = liked,
			GenreIds = genres.ToList(),
			CategoryIds = (categories ?? []).ToList()
		};

		private static Song SongWith ( int[] genres, int[]? categories = null, DateTime? created = null )
		{
			var song = new Song { Id = Guid.NewGuid(), Name = "S", Url = "u", CreatedAt = created ?? Now };
			foreach(var g in genres)
				song.Genres.Add(new Genre { Id = g, Name = "G" });
			foreach(var c in categories ?? [])
				song.Categories.Add(new Category { Id = c, Name = "C" });
			return song;
		}

		[Fact]
		public void BuildProfile_NoSignals_ReturnsEmptyProfile ( )
		{
			var profile = ContentBasedScorer.BuildProfile(new List<ListenSignal>(), Now);

			Assert.Empty(profile.Genres);
			Assert.Empty(profile.Categories);
		}

		[Fact]
		public void BuildProfile_NormalizesStrongestTagToOne ( )
		{
			var profile = ContentBasedScorer.BuildProfile(new List<ListenSignal>
		{
			Signal([1], percent: 100, liked: true),
			Signal([2], percent: 40)
		}, Now);

			Assert.Equal(1.0, profile.Genres[1], 6);
			Assert.InRange(profile.Genres[2], 0.0, 1.0);
			Assert.True(profile.Genres[2] < profile.Genres[1]);
		}

		[Fact]
		public void BuildProfile_FullyPlayedAndLiked_OutweighsSkipped ( )
		{
			var profile = ContentBasedScorer.BuildProfile(new List<ListenSignal>
		{
			Signal([1], percent: 100, liked: true),
			Signal([2], percent: 10)
		}, Now);

			Assert.True(profile.Genres[1] > profile.Genres[2]);
		}

		[Fact]
		public void BuildProfile_OldListen_WeighsLessThanRecent ( )
		{
			var profile = ContentBasedScorer.BuildProfile(new List<ListenSignal>
		{
			Signal([1], percent: 50, ageDays: 0),
			Signal([2], percent: 50, ageDays: 90)
		}, Now);

			Assert.True(profile.Genres[2] < profile.Genres[1]);
		}

		[Fact]
		public void BuildProfile_PercentAbove100_IsClamped ( )
		{
			var profile = ContentBasedScorer.BuildProfile(new List<ListenSignal>
		{
			Signal([1], percent: 500),
			Signal([2], percent: 100)
		}, Now);

			Assert.Equal(profile.Genres[1], profile.Genres[2], 6);
		}

		[Fact]
		public void BuildProfile_CategoriesAreCountedSeparately ( )
		{
			var profile = ContentBasedScorer.BuildProfile(new List<ListenSignal>
		{
			Signal([1], categories: [7])
		}, Now);

			Assert.Equal(1.0, profile.Categories[7], 6);
			Assert.False(profile.Categories.ContainsKey(1));
		}

		[Fact]
		public void TopIds_ReturnsOnlyPositiveWeightsDescendingLimitedByCount ( )
		{
			var weights = new Dictionary<int, double> { [1] = 0.5, [2] = 1.0, [3] = -1.0, [4] = 0.2, [5] = 0.0 };

			var result = ContentBasedScorer.TopIds(weights, 2);

			Assert.Equal(new[] { 2, 1 }, result.ToArray());
		}

		[Fact]
		public void TopIds_Empty_ReturnsEmpty ( )
		{
			Assert.Empty(ContentBasedScorer.TopIds(new Dictionary<int, double>(), 5));
		}

		[Fact]
		public void Score_SongWithoutTags_IsZero ( )
		{
			var profile = new ContentBasedScorer.Profile(new Dictionary<int, double> { [1] = 1 }, new Dictionary<int, double>());

			Assert.Equal(0, ContentBasedScorer.Score(SongWith([]), profile, Now));
		}

		[Fact]
		public void Score_UnknownTags_IsZero ( )
		{
			var profile = new ContentBasedScorer.Profile(new Dictionary<int, double> { [1] = 1 }, new Dictionary<int, double>());

			Assert.Equal(0, ContentBasedScorer.Score(SongWith([99], [99]), profile, Now));
		}

		[Fact]
		public void Score_BrandNewSongWithTopGenre_UsesGenreWeightAndFullFreshnessBonus ( )
		{
			var profile = new ContentBasedScorer.Profile(new Dictionary<int, double> { [1] = 1 }, new Dictionary<int, double>());

			var score = ContentBasedScorer.Score(SongWith([1]), profile, Now);

			Assert.Equal(0.6 * 1.5, score, 6);
		}

		[Fact]
		public void Score_UsesBestMatchingGenreAndCategory ( )
		{
			var profile = new ContentBasedScorer.Profile(
				new Dictionary<int, double> { [1] = 1.0, [2] = 0.2 },
				new Dictionary<int, double> { [5] = 0.5 });

			var score = ContentBasedScorer.Score(SongWith([1, 2], [5]), profile, Now);

			Assert.Equal((0.6 * 1.0 + 0.4 * 0.5) * 1.5, score, 6);
		}

		[Fact]
		public void Score_NewerSong_ScoresHigherWithSameTags ( )
		{
			var profile = new ContentBasedScorer.Profile(new Dictionary<int, double> { [1] = 1 }, new Dictionary<int, double>());

			var fresh = ContentBasedScorer.Score(SongWith([1], created: Now), profile, Now);
			var old = ContentBasedScorer.Score(SongWith([1], created: Now.AddDays(-60)), profile, Now);

			Assert.True(fresh > old);
		}
	}
}

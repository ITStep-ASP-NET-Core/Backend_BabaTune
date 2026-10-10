using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Common
{
	public static class ContentBasedScorer
	{
		private const double HistoryDecayDays = 30;
		private const double FreshnessDecayDays = 7;
		private const double FreshnessBonus = 0.5;

		public sealed record Profile ( Dictionary<int, double> Genres, Dictionary<int, double> Categories );

		public static Profile BuildProfile ( IReadOnlyCollection<ListenSignal> signals, DateTime now )
		{
			var genres = new Dictionary<int, double>();
			var categories = new Dictionary<int, double>();

			foreach(var s in signals)
			{
				var ageDays = Math.Max(0, (now - s.ListenedAt).TotalDays);
				var decay = Math.Exp(-ageDays / HistoryDecayDays);
				var engagement = 0.7 * Math.Clamp(s.PlayedPercent / 100.0, 0, 1) + 0.3 * (s.IsLiked ? 1 : 0);
				var weight = decay * engagement;

				foreach(var id in s.GenreIds)
					genres[id] = genres.GetValueOrDefault(id) + weight;

				foreach(var id in s.CategoryIds)
					categories[id] = categories.GetValueOrDefault(id) + weight;
			}

			Normalize(genres);
			Normalize(categories);
			return new Profile(genres, categories);
		}

		public static List<int> TopIds ( Dictionary<int, double> weights, int count )
			=> weights.Where(x => x.Value > 0)
				.OrderByDescending(x => x.Value)
				.Take(count)
				.Select(x => x.Key)
				.ToList();

		public static double Score ( Song song, Profile profile, DateTime now )
		{
			var genre = song.Genres.Select(g => profile.Genres.GetValueOrDefault(g.Id)).DefaultIfEmpty(0).Max();
			var category = song.Categories.Select(c => profile.Categories.GetValueOrDefault(c.Id)).DefaultIfEmpty(0).Max();
			var tagScore = 0.6 * genre + 0.4 * category;

			var ageDays = Math.Max(0, (now - song.CreatedAt).TotalDays);
			var freshness = 1 + FreshnessBonus * Math.Exp(-ageDays / FreshnessDecayDays);

			return tagScore * freshness;
		}

		private static void Normalize ( Dictionary<int, double> weights )
		{
			if(weights.Count == 0)
				return;

			var max = weights.Values.Max();
			if(max <= 0)
				return;

			foreach(var key in weights.Keys.ToList())
				weights[key] /= max;
		}
	}
}

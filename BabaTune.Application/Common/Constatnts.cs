
namespace BabaTune.Application.Common
{
	public static class Constants
	{
		public static class Limits
		{
			public const int MaxPlaylistSongs = 500;
			public const int MaxAlbumSongs = 100;
			public const int MaxTextLength = 2000;
			public const int MaxPageSize = 100;
			public const long MaxImageSize = 5L * 1024 * 1024;
			public const long MaxAudioSize = 50L * 1024 * 1024;
		}

		public static class Defaults
		{
			private const string BaseUrl = "https://storage.babatune.app/defaults";

			public const string AlbumImageUrl = BaseUrl + "/album-cover.png";
			public const string CategoryImageUrl = BaseUrl + "/category-cover.png";
			public const string GenreImageUrl = BaseUrl + "/genre-cover.png";
			public const string PlaylistImageUrl = BaseUrl + "/playlist-cover.png";
			public const string SongImageUrl = BaseUrl + "/song-cover.png";
		}

		public static class Folders
		{
			public const string AlbumCovers = "albums/covers";
			public const string CategoryCovers = "categories/covers";
			public const string GenreCovers = "genres/covers";
			public const string PlaylistCovers = "playlists/covers";
			public const string SongCovers = "songs/covers";
			public const string SongAudio = "songs/audio";
			public const string UserAvatars = "users/avatars";
			public const string NoticeImages = "notices/images";
		}

		public static class Argon2
		{
			public const int SaltSize = 16;
			public const int HashSize = 32;
			public const int DegreeOfParallelism = 8;
			public const int Iterations = 4;
			public const int MemorySize = 1024 * 128;
		}

		public static class Recommendations
		{
			public const int PoolSize = 200;
			public const int ExplorationReserve = 20;
			public const int HistoryTake = 400;
			public const int CandidatesTake = 400;
			public const int TopTagsCount = 5;
			public const int MaxPerAuthorInPool = 3;
			public const int MaxPerAuthorPerPage = 2;
			public const int SubscriptionPercent = 20;
			public const int SubscriptionCandidates = 200;
			public const int SubscriptionPerAuthor = 3;
			public const int SubscriptionShortlist = 10;
			public const int SubscriptionFreshHours = 48;
			public const int PopularListSize = 300;
			public const double ExplorationScore = 0.15;
			public const double PopularityWeight = 0.5;

			public static readonly TimeSpan PoolTtl = TimeSpan.FromMinutes(5);
			public static readonly TimeSpan PopularTtl = TimeSpan.FromMinutes(10);
			public static readonly TimeSpan SeenSubscriptionsTtl = TimeSpan.FromMinutes(30);
		}

		public static class Scoring
		{
			public const double HistoryDecayDays = 30;
			public const double FreshnessDecayDays = 7;
			public const double FreshnessBonus = 0.5;
		}
	}
}
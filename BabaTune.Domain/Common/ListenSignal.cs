
namespace BabaTune.Domain.Common
{
	public class ListenSignal
	{
		public Guid SongId { get; init; }
		public DateTime ListenedAt { get; init; }
		public double PlayedPercent { get; init; }
		public bool IsLiked { get; init; }
		public List<int> GenreIds { get; init; } = [];
		public List<int> CategoryIds { get; init; } = [];
	}
}

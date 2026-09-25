
namespace BabaTune.Domain.Entities
{

    public class PlaylistItem
    {
        public Guid Id { get; set; }
        public Guid PlaylistId { get; set; }
        public Playlist Playlist { get; set; } = null!;
		public Guid SongId { get; set; }
		public Song Song { get; set; } = null!;
		public int Number { get; set; }
        public DateTime AddedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
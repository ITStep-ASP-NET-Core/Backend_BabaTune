
namespace BabaTune.Domain.Entities
{
    public class ListenHistory
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public Guid SongId { get; set; }
        public Song? Song { get; set; }
        public bool IsLiked { get; set; } = false;
        public int PlayedPercent { get; set; } = 0;
        public DateTime ListenedAt { get; set; } = DateTime.UtcNow;
    }
}
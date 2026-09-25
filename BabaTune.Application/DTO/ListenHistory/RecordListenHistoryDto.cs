namespace BabaTune.Application.DTO.ListenHistory
{
	public class RecordListenHistoryDto
	{
		public Guid UserId { get; set; }
		public Guid SongId { get; set; }
		public int PlayedPercent { get; set; }
	}
}

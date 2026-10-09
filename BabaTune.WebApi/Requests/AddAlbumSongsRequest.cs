namespace BabaTune.WebApi.Requests
{
	public class AddAlbumSongsRequest
	{
		public ICollection<Guid> SongIds { get; set; } = [];
	}
}

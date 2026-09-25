namespace BabaTune.Infrastructure.Interfaces
{
	public interface IStorageRepository
	{
		Task<string> UploadAsync ( Stream fileStream, string fileName, string contentType, string folder );
		Task<string> EditAsync ( string fileUrl, Stream fileStream, string contentType );
		Task DeleteAsync ( string fileUrl );
	}
}

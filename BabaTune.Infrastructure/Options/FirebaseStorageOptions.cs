namespace BabaTune.Infrastructure.Options
{
	public class FirebaseStorageOptions
	{
		public string BucketName { get; set; } = string.Empty;
		public string? Credentials { get; set; }
	}
}

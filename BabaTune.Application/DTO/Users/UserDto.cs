namespace BabaTune.Application.DTO.Users
{
	public class UserDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? AvatarUrl { get; set; }
		public int SubscriptionsCount { get; set; }
		public int TracksCount { get; set; }
		public int ListenersCount { get; set; }
		public bool IsChecked { get; set; }
	}
}

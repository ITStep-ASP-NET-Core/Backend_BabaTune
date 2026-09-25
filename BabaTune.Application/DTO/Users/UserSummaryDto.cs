namespace BabaTune.Application.DTO.Users
{
	public class UserSummaryDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? AvatarUrl { get; set; }
	}
}

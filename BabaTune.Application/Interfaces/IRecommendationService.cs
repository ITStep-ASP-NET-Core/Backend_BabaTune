using BabaTune.Application.DTO.Songs;

namespace BabaTune.Application.Interfaces
{
	public interface IRecommendationService
	{
		Task<FeedPageDto> GetFeedAsync ( Guid userId, int take );
	}
}

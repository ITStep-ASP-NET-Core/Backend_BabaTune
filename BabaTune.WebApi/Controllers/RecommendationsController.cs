using BabaTune.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers
{ 
	[Route("api/recommendations")]
	public class RecommendationsController : BaseApiController
	{
		private readonly IRecommendationService _recommendationService;

		public RecommendationsController ( IRecommendationService recommendationService )
		{
			_recommendationService = recommendationService;
		}

		[HttpGet]
		public async Task<IActionResult> Get ( [FromQuery] int take = 20 )
			=> Ok(await _recommendationService.GetFeedAsync(CurrentUserId, Math.Clamp(take, 1, 50)));
	}
}
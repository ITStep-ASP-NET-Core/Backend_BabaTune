using BabaTune.Application.DTO.Songs;
using BabaTune.Application.Interfaces;
using BabaTune.Tests.Common;
using BabaTune.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BabaTune.Tests.Controllers
{
	public class RecommendationsControllerTests
	{
		private readonly Mock<IRecommendationService> _service = new();
		private readonly Guid _userId = Guid.NewGuid();
		private readonly RecommendationsController _sut;

		public RecommendationsControllerTests ( )
		{
			_service
				.Setup(s => s.GetFeedAsync(It.IsAny<Guid>(), It.IsAny<int>()))
				.ReturnsAsync(new FeedPageDto());

			_sut = new RecommendationsController(_service.Object).WithUser(_userId);
		}

		[Fact]
		public async Task Get_ReturnsOk ( )
		{
			var result = await _sut.Get();

			Assert.IsType<OkObjectResult>(result);
		}

		[Fact]
		public async Task Get_PassesCurrentUserAndDefaultTake ( )
		{
			await _sut.Get();

			_service.Verify(s => s.GetFeedAsync(_userId, 20), Times.Once);
		}

		[Theory]
		[InlineData(0, 1)]
		[InlineData(-5, 1)]
		[InlineData(1000, 50)]
		[InlineData(30, 30)]
		public async Task Get_ClampsTake ( int take, int expected )
		{
			await _sut.Get(take);

			_service.Verify(s => s.GetFeedAsync(_userId, expected), Times.Once);
		}
	}
}

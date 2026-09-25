using BabaTune.Application.Common;
using BabaTune.Application.DTO.ListenHistory;
using BabaTune.Application.DTO.Songs;
using BabaTune.Application.Interfaces;
using BabaTune.Domain.Common;
using BabaTune.Tests.Common;
using BabaTune.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BabaTune.Tests.Controllers;

public class ListenHistoryControllerTests
{
	private readonly Mock<IListenHistoryService> _service = new();
	private readonly Guid _userId = Guid.NewGuid();
	private readonly ListenHistoryController _sut;

	public ListenHistoryControllerTests ( )
	{
		_sut = new ListenHistoryController(_service.Object).WithUser(_userId);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(101)]
	public async Task Record_PercentOutOfRange_ReturnsBadRequestWithoutCallingService ( int percent )
	{
		var result = await _sut.Record(new RecordListenHistoryDto { SongId = Guid.NewGuid(), PlayedPercent = percent });

		var problem = Assert.IsAssignableFrom<ObjectResult>(result);
		Assert.Equal(400, problem.StatusCode);
		_service.Verify(s => s.RecordAsync(It.IsAny<RecordListenHistoryDto>()), Times.Never);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(100)]
	public async Task Record_BoundaryPercent_IsAccepted ( int percent )
	{
		_service.Setup(s => s.RecordAsync(It.IsAny<RecordListenHistoryDto>())).ReturnsAsync(Result.Ok());

		var result = await _sut.Record(new RecordListenHistoryDto { SongId = Guid.NewGuid(), PlayedPercent = percent });

		Assert.IsType<NoContentResult>(result);
	}

	[Fact]
	public async Task Record_OverridesUserIdFromToken ( )
	{
		RecordListenHistoryDto? captured = null;
		_service
			.Setup(s => s.RecordAsync(It.IsAny<RecordListenHistoryDto>()))
			.Callback<RecordListenHistoryDto>(d => captured = d)
			.ReturnsAsync(Result.Ok());

		await _sut.Record(new RecordListenHistoryDto { UserId = Guid.NewGuid(), SongId = Guid.NewGuid(), PlayedPercent = 50 });

		Assert.NotNull(captured);
		Assert.Equal(_userId, captured.UserId);
	}

	[Fact]
	public async Task Record_ServiceFailure_ReturnsMappedStatus ( )
	{
		_service.Setup(s => s.RecordAsync(It.IsAny<RecordListenHistoryDto>())).ReturnsAsync(Result.Fail("Song not found."));

		var result = await _sut.Record(new RecordListenHistoryDto { SongId = Guid.NewGuid(), PlayedPercent = 10 });

		var problem = Assert.IsAssignableFrom<ObjectResult>(result);
		Assert.Equal(404, problem.StatusCode);
	}
}

public class SongsControllerTests
{
	private readonly Mock<ISongService> _service = new();
	private readonly Guid _userId = Guid.NewGuid();
	private readonly SongsController _sut;

	public SongsControllerTests ( )
	{
		_sut = new SongsController(_service.Object).WithUser(_userId);
	}

	[Fact]
	public async Task GetTop_UnknownPeriod_ReturnsBadRequest ( )
	{
		var result = await _sut.GetTop((TopPeriod)99);

		var problem = Assert.IsAssignableFrom<ObjectResult>(result);
		Assert.Equal(400, problem.StatusCode);
		_service.Verify(s => s.GetTopAsync(It.IsAny<TopPeriod>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>()), Times.Never);
	}

	[Fact]
	public async Task GetTop_ValidPeriod_ReturnsOk ( )
	{
		_service
			.Setup(s => s.GetTopAsync(TopPeriod.Day, 1, 20, _userId))
			.ReturnsAsync(new PagedResult<SongDto>());

		var result = await _sut.GetTop(TopPeriod.Day);

		Assert.IsType<OkObjectResult>(result);
	}

	[Fact]
	public async Task GetByFilters_ClampsPaging ( )
	{
		var filter = new SongFilterDto();
		var anonymous = new SongsController(_service.Object).WithUser(null);
		_service
			.Setup(s => s.GetByFiltersAsync(filter, 1, 100, null))
			.ReturnsAsync(new PagedResult<SongDto>());

		var result = await anonymous.GetByFilters(filter, pageNumber: 0, pageSize: 1000);

		Assert.IsType<OkObjectResult>(result);
		_service.Verify(s => s.GetByFiltersAsync(filter, 1, 100, null), Times.Once);
	}

	[Fact]
	public async Task Create_NonAudioFile_ReturnsBadRequestWithoutCallingService ( )
	{
		var dto = new CreateSongDto { AudioFile = FormFiles.Create("a.png", "image/png") };

		var result = await _sut.Create(dto);

		var problem = Assert.IsAssignableFrom<ObjectResult>(result);
		Assert.Equal(400, problem.StatusCode);
		_service.Verify(s => s.CreateAsync(It.IsAny<CreateSongDto>(), It.IsAny<Guid>()), Times.Never);
	}

	[Fact]
	public async Task Create_NonImageCover_ReturnsBadRequestWithoutCallingService ( )
	{
		var dto = new CreateSongDto
		{
			AudioFile = FormFiles.Create("a.mp3", "audio/mpeg"),
			ImageFile = FormFiles.Create("c.txt", "text/plain")
		};

		var result = await _sut.Create(dto);

		var problem = Assert.IsAssignableFrom<ObjectResult>(result);
		Assert.Equal(400, problem.StatusCode);
		_service.Verify(s => s.CreateAsync(It.IsAny<CreateSongDto>(), It.IsAny<Guid>()), Times.Never);
	}

	[Fact]
	public async Task Create_ValidFiles_Returns201AndPassesCurrentUser ( )
	{
		var dto = new CreateSongDto { AudioFile = FormFiles.Create("a.mp3", "audio/mpeg") };
		_service.Setup(s => s.CreateAsync(dto, _userId)).ReturnsAsync(Result.Ok());

		var result = await _sut.Create(dto);

		var status = Assert.IsType<StatusCodeResult>(result);
		Assert.Equal(201, status.StatusCode);
	}

	[Fact]
	public async Task Delete_NotAuthor_Returns403 ( )
	{
		var id = Guid.NewGuid();
		_service.Setup(s => s.DeleteAsync(id, _userId)).ReturnsAsync(Result.Fail("You are not the author of this song."));

		var result = await _sut.Delete(id);

		var problem = Assert.IsAssignableFrom<ObjectResult>(result);
		Assert.Equal(403, problem.StatusCode);
	}
}
using BabaTune.Application.DTO.ListenHistory;
using BabaTune.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/listen-history")]
public class ListenHistoryController : BaseApiController
{
	private readonly IListenHistoryService _listenHistoryService;

	public ListenHistoryController ( IListenHistoryService listenHistoryService )
	{
		_listenHistoryService = listenHistoryService;
	}

	[HttpGet]
	public async Task<IActionResult> GetHistory ( [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _listenHistoryService.GetHistoryAsync(CurrentUserId, page, size));
	}

	[HttpPost]
	public async Task<IActionResult> Record ( [FromBody] RecordListenHistoryDto dto )
	{
		if(dto.PlayedPercent is < 0 or > 100)
			return Problem(detail: "PlayedPercent must be between 0 and 100.", statusCode: StatusCodes.Status400BadRequest);

		dto.UserId = CurrentUserId;
		return ToActionResult(await _listenHistoryService.RecordAsync(dto));
	}
}
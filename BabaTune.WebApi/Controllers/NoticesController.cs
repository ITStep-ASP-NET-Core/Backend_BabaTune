using BabaTune.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/notices")]
public class NoticesController : BaseApiController
{
	private readonly INoticeService _noticeService;

	public NoticesController ( INoticeService noticeService )
	{
		_noticeService = noticeService;
	}

	[HttpGet]
	public async Task<IActionResult> Get ( [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _noticeService.GetByRecipientAsync(CurrentUserId, page, size));
	}

	[HttpGet("unread-count")]
	public async Task<IActionResult> GetUnreadCount ( )
		=> Ok(new { count = await _noticeService.GetUnreadCountAsync(CurrentUserId) });

	[HttpPost("{id:guid}/read")]
	public async Task<IActionResult> MarkAsRead ( Guid id )
		=> ToActionResult(await _noticeService.MarkAsReadAsync(id, CurrentUserId));
}

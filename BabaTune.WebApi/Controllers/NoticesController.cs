using BabaTune.Application.DTO.Notices;
using BabaTune.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static BabaTune.Application.Common.Constants;

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
	public async Task<IActionResult> GetAll ( [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _noticeService.GetAllAsync(CurrentUserId, page, size));
	}

	[HttpGet("unread")]
	public async Task<IActionResult> GetUnread ( [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _noticeService.GetUnreadAsync(CurrentUserId, page, size));
	}

	[HttpGet("unread-count")]
	public async Task<IActionResult> GetUnreadCount ( )
		=> Ok(new { count = await _noticeService.GetUnreadCountAsync(CurrentUserId) });

	[HttpPost("{id:guid}/read")]
	public async Task<IActionResult> MarkAsRead ( Guid id )
		=> ToActionResult(await _noticeService.MarkAsReadAsync(id, CurrentUserId));

	[HttpPost("server")]
	[Authorize(Policy = "AdminOnly")]
	[Consumes("multipart/form-data")]
	[RequestSizeLimit(Limits.MaxImageSize)]
	public async Task<IActionResult> CreateServer ( [FromForm] CreateServerNoticeDto dto )
	{
		if(ValidateImage(dto.ImageFile) is { } error)
			return error;

		return ToActionResult(await _noticeService.CreateServerAsync(dto), created: true);
	}

	[HttpPut("server/{id:guid}")]
	[Authorize(Policy = "AdminOnly")]
	public async Task<IActionResult> UpdateServer ( Guid id, [FromBody] UpdateServerNoticeDto dto )
		=> ToActionResult(await _noticeService.UpdateServerAsync(id, dto));

	[HttpDelete("server/{id:guid}")]
	[Authorize(Policy = "AdminOnly")]
	public async Task<IActionResult> DeleteServer ( Guid id )
		=> ToActionResult(await _noticeService.DeleteServerAsync(id));
}
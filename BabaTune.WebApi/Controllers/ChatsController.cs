using BabaTune.Application.Interfaces;
using BabaTune.WebApi.Requests;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/chats")]
public class ChatsController : BaseApiController
{
	private readonly IChatService _chatService;

	public ChatsController ( IChatService chatService )
	{
		_chatService = chatService;
	}

	[HttpGet("{chatId:guid}/messages")]
	public async Task<IActionResult> GetMessages ( Guid chatId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		var result = await _chatService.GetMessagesAsync(chatId, CurrentUserId, page, size);

		return result.Success ? Ok(result.Data) : Failure(result.Error);
	}

	[HttpPost("{chatId:guid}/messages/text")]
	public async Task<IActionResult> SendText ( Guid chatId, [FromBody] SendTextRequest request )
	{
		var result = await _chatService.SendTextAsync(chatId, CurrentUserId, request.Text);

		return result.Success ? StatusCode(StatusCodes.Status201Created, result.Data) : Failure(result.Error);
	}

	[HttpPost("{chatId:guid}/messages/song")]
	public async Task<IActionResult> SendSong ( Guid chatId, [FromBody] SendSongRequest request )
	{
		var result = await _chatService.SendSongAsync(chatId, CurrentUserId, request.SongId);

		return result.Success ? StatusCode(StatusCodes.Status201Created, result.Data) : Failure(result.Error);
	}
}

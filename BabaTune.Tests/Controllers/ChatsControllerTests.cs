using BabaTune.Application.Common;
using BabaTune.Application.DTO.Chats;
using BabaTune.Application.Interfaces;
using BabaTune.Domain.Common;
using BabaTune.Tests.Common;
using BabaTune.WebApi.Controllers;
using BabaTune.WebApi.Requests;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BabaTune.Tests.Controllers;

public class ChatsControllerTests
{
    private readonly Mock<IChatService> _service = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _chatId = Guid.NewGuid();
    private readonly ChatsController _sut;

    public ChatsControllerTests()
    {
        _sut = new ChatsController(_service.Object).WithUser(_userId);
    }

    private static void AssertStatus(IActionResult result, int status)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(status, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetMessages_Success_ReturnsOkWithPage()
    {
        var page = new PagedResult<MessageDto>();
        _service.Setup(s => s.GetMessagesAsync(_chatId, _userId, 1, 50)).ReturnsAsync(Result.Ok(page));

        var result = await _sut.GetMessages(_chatId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(page, ok.Value);
    }

    [Fact]
    public async Task GetMessages_ClampsPaging()
    {
        _service.Setup(s => s.GetMessagesAsync(_chatId, _userId, 1, 100)).ReturnsAsync(Result.Ok(new PagedResult<MessageDto>()));

        var result = await _sut.GetMessages(_chatId, 0, 1000);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetMessagesAsync(_chatId, _userId, 1, 100), Times.Once);
    }

    [Fact]
    public async Task GetMessages_NotParticipant_Returns404()
    {
        _service
            .Setup(s => s.GetMessagesAsync(_chatId, _userId, 1, 50))
            .ReturnsAsync(Result<PagedResult<MessageDto>>.Fail("Chat not found."));

        var result = await _sut.GetMessages(_chatId);

        AssertStatus(result, 404);
    }

    [Fact]
    public async Task SendText_Success_Returns201WithMessage()
    {
        var message = new MessageDto { Id = Guid.NewGuid(), Text = "hi" };
        _service.Setup(s => s.SendTextAsync(_chatId, _userId, "hi")).ReturnsAsync(Result.Ok(message));

        var result = await _sut.SendText(_chatId, new SendTextRequest { Text = "hi" });

        var created = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Same(message, created.Value);
    }

    [Theory]
    [InlineData("Message text is required.", 400)]
    [InlineData("Message text is too long.", 400)]
    [InlineData("Chat not found.", 404)]
    public async Task SendText_Failure_MapsStatus(string error, int expectedStatus)
    {
        _service.Setup(s => s.SendTextAsync(_chatId, _userId, It.IsAny<string>())).ReturnsAsync(Result<MessageDto>.Fail(error));

        var result = await _sut.SendText(_chatId, new SendTextRequest { Text = "x" });

        AssertStatus(result, expectedStatus);
    }

    [Fact]
    public async Task SendSong_Success_Returns201WithMessage()
    {
        var songId = Guid.NewGuid();
        var message = new MessageDto { Id = Guid.NewGuid() };
        _service.Setup(s => s.SendSongAsync(_chatId, _userId, songId)).ReturnsAsync(Result.Ok(message));

        var result = await _sut.SendSong(_chatId, new SendSongRequest { SongId = songId });

        var created = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Same(message, created.Value);
    }

    [Fact]
    public async Task SendSong_SongMissing_Returns404()
    {
        var songId = Guid.NewGuid();
        _service.Setup(s => s.SendSongAsync(_chatId, _userId, songId)).ReturnsAsync(Result<MessageDto>.Fail("Song not found."));

        var result = await _sut.SendSong(_chatId, new SendSongRequest { SongId = songId });

        AssertStatus(result, 404);
    }
}

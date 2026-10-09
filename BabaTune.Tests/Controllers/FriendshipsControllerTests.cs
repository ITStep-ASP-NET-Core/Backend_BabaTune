using BabaTune.Application.Common;
using BabaTune.Application.DTO.Friendships;
using BabaTune.Application.Interfaces;
using BabaTune.Domain.Common;
using BabaTune.Tests.Common;
using BabaTune.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BabaTune.Tests.Controllers;

public class FriendshipsControllerTests
{
    private readonly Mock<IFriendshipService> _service = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FriendshipsController _sut;

    public FriendshipsControllerTests()
    {
        _sut = new FriendshipsController(_service.Object).WithUser(_userId);
    }

    private static void AssertStatus(IActionResult result, int status)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(status, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetFriends_ClampsPagingAndUsesCurrentUser()
    {
        _service.Setup(s => s.GetFriendsAsync(_userId, 1, 100)).ReturnsAsync(new PagedResult<FriendshipDto>());

        var result = await _sut.GetFriends(0, 1000);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetFriendsAsync(_userId, 1, 100), Times.Once);
    }

    [Fact]
    public async Task GetIncoming_UsesCurrentUser()
    {
        _service.Setup(s => s.GetIncomingAsync(_userId, 2, 5)).ReturnsAsync(new PagedResult<FriendshipDto>());

        var result = await _sut.GetIncoming(2, 5);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetIncomingAsync(_userId, 2, 5), Times.Once);
    }

    [Fact]
    public async Task GetBlocked_UsesCurrentUser()
    {
        _service.Setup(s => s.GetBlockedAsync(_userId, 1, 20)).ReturnsAsync(new PagedResult<FriendshipDto>());

        var result = await _sut.GetBlocked();

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetBlockedAsync(_userId, 1, 20), Times.Once);
    }

    [Fact]
    public async Task SendRequest_Success_Returns201()
    {
        var targetId = Guid.NewGuid();
        _service.Setup(s => s.SendRequestAsync(_userId, targetId)).ReturnsAsync(Result.Ok());

        var result = await _sut.SendRequest(targetId);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(201, status.StatusCode);
    }

    [Theory]
    [InlineData("User not found.", 404)]
    [InlineData("Already friends.", 409)]
    [InlineData("Friend request already exists.", 409)]
    [InlineData("You cannot send a friend request to yourself.", 400)]
    public async Task SendRequest_Failure_MapsStatus(string error, int expectedStatus)
    {
        var targetId = Guid.NewGuid();
        _service.Setup(s => s.SendRequestAsync(_userId, targetId)).ReturnsAsync(Result.Fail(error));

        var result = await _sut.SendRequest(targetId);

        AssertStatus(result, expectedStatus);
    }

    [Fact]
    public async Task Accept_Success_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.AcceptAsync(id, _userId)).ReturnsAsync(Result.Ok());

        var result = await _sut.Accept(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Accept_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.AcceptAsync(id, _userId)).ReturnsAsync(Result.Fail("Friend request not found."));

        var result = await _sut.Accept(id);

        AssertStatus(result, 404);
    }

    [Fact]
    public async Task Block_Success_ReturnsNoContent()
    {
        var targetId = Guid.NewGuid();
        _service.Setup(s => s.BlockAsync(_userId, targetId)).ReturnsAsync(Result.Ok());

        var result = await _sut.Block(targetId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Block_AlreadyBlocked_Returns409()
    {
        var targetId = Guid.NewGuid();
        _service.Setup(s => s.BlockAsync(_userId, targetId)).ReturnsAsync(Result.Fail("Already blocked."));

        var result = await _sut.Block(targetId);

        AssertStatus(result, 409);
    }

    [Fact]
    public async Task Remove_Success_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.RemoveAsync(id, _userId)).ReturnsAsync(Result.Ok());

        var result = await _sut.Remove(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Remove_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.RemoveAsync(id, _userId)).ReturnsAsync(Result.Fail("Friendship not found."));

        var result = await _sut.Remove(id);

        AssertStatus(result, 404);
    }
}

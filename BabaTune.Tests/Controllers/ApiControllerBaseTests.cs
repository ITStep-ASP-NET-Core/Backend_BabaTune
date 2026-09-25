using BabaTune.Application.Common;
using BabaTune.Tests.Common;
using BabaTune.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.Tests.Controllers;

public class ApiControllerBaseTests
{
    private sealed class TestController : BaseApiController
    {
        public IActionResult CallFailure(string? error) => Failure(error);
        public IActionResult CallToActionResult(Result result, bool created = false) => ToActionResult(result, created);
        public IActionResult CallOkOrNotFound<T>(T? value) where T : class => OkOrNotFound(value);
        public IActionResult? CallValidateImage(IFormFile? file, bool required = false) => ValidateImage(file, required);
        public IActionResult? CallValidateAudio(IFormFile? file, bool required = false) => ValidateAudio(file, required);
        public Guid ExposedUserId => CurrentUserId;
        public Guid? ExposedUserIdOrNull => CurrentUserIdOrNull;
        public static (int Page, int Size) CallPaging(int pageNumber, int pageSize) => Paging(pageNumber, pageSize);
    }

    private static void AssertStatus(IActionResult? result, int status)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(status, objectResult.StatusCode);
    }

    [Theory]
    [InlineData("Song not found.", 404)]
    [InlineData("Playlist not found.", 404)]
    [InlineData("You are not the author of this song.", 403)]
    [InlineData("You are not the owner of this playlist.", 403)]
    [InlineData("This notice does not belong to you.", 403)]
    [InlineData("Already subscribed.", 409)]
    [InlineData("Email already in use.", 409)]
    [InlineData("Position must be at least 1.", 400)]
    [InlineData("Liked playlist cannot be deleted.", 400)]
    [InlineData("Current password is incorrect.", 400)]
    [InlineData(null, 400)]
    public void Failure_MapsErrorMessageToStatusCode(string? error, int expectedStatus)
    {
        var result = new TestController().CallFailure(error);

        AssertStatus(result, expectedStatus);
    }

    [Fact]
    public void ToActionResult_Success_ReturnsNoContent()
    {
        var result = new TestController().CallToActionResult(Result.Ok());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void ToActionResult_SuccessWithCreated_Returns201()
    {
        var result = new TestController().CallToActionResult(Result.Ok(), created: true);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(201, status.StatusCode);
    }

    [Fact]
    public void ToActionResult_Failure_ReturnsMappedProblem()
    {
        var result = new TestController().CallToActionResult(Result.Fail("Song not found."));

        AssertStatus(result, 404);
    }

    [Fact]
    public void OkOrNotFound_Null_ReturnsNotFound()
    {
        var result = new TestController().CallOkOrNotFound<string>(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void OkOrNotFound_Value_ReturnsOk()
    {
        var result = new TestController().CallOkOrNotFound("value");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("value", ok.Value);
    }

    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-3, 500, 1, 100)]
    [InlineData(4, 20, 4, 20)]
    [InlineData(2, 100, 2, 100)]
    public void Paging_ClampsValues(int pageNumber, int pageSize, int expectedPage, int expectedSize)
    {
        var (page, size) = TestController.CallPaging(pageNumber, pageSize);

        Assert.Equal(expectedPage, page);
        Assert.Equal(expectedSize, size);
    }

    [Theory]
    [InlineData("image/png", 10, true)]
    [InlineData("IMAGE/JPEG", 10, true)]
    [InlineData("text/plain", 10, false)]
    [InlineData("audio/mpeg", 10, false)]
    [InlineData("image/png", 0, false)]
    public void ValidateImage_ChecksContentTypeAndSize(string contentType, long length, bool valid)
    {
        var result = new TestController().CallValidateImage(FormFiles.Create("f", contentType, length));

        if (valid)
            Assert.Null(result);
        else
            AssertStatus(result, 400);
    }

    [Theory]
    [InlineData("audio/mpeg", 10, true)]
    [InlineData("audio/wav", 10, true)]
    [InlineData("image/png", 10, false)]
    [InlineData("audio/mpeg", 0, false)]
    public void ValidateAudio_ChecksContentTypeAndSize(string contentType, long length, bool valid)
    {
        var result = new TestController().CallValidateAudio(FormFiles.Create("f", contentType, length));

        if (valid)
            Assert.Null(result);
        else
            AssertStatus(result, 400);
    }

    [Fact]
    public void ValidateImage_NullOptional_ReturnsNull()
    {
        Assert.Null(new TestController().CallValidateImage(null));
    }

    [Fact]
    public void ValidateImage_NullRequired_ReturnsBadRequest()
    {
        AssertStatus(new TestController().CallValidateImage(null, required: true), 400);
    }

    [Fact]
    public void CurrentUserId_ReadsGuidFromClaim()
    {
        var userId = Guid.NewGuid();
        var controller = new TestController().WithUser(userId);

        Assert.Equal(userId, controller.ExposedUserId);
        Assert.Equal(userId, controller.ExposedUserIdOrNull);
    }

    [Fact]
    public void CurrentUserIdOrNull_WithoutClaim_ReturnsNull()
    {
        var controller = new TestController().WithUser(null);

        Assert.Null(controller.ExposedUserIdOrNull);
    }
}

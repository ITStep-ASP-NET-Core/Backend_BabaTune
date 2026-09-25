using BabaTune.Application.Implementations;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Moq;

namespace BabaTune.Tests.Services;

public class NoticeServiceTests
{
    private readonly UowMock _uow = new();
    private readonly NoticeService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public NoticeServiceTests()
    {
        _sut = new NoticeService(_uow.Object);
    }

    [Fact]
    public async Task MarkAsRead_Missing_Fails()
    {
        var result = await _sut.MarkAsReadAsync(Guid.NewGuid(), _userId);

        Assert.False(result.Success);
        Assert.Equal("Notice not found.", result.Error);
    }

    [Fact]
    public async Task MarkAsRead_ForeignNotice_Fails()
    {
        var notice = new Notice { Id = Guid.NewGuid(), RecipientId = Guid.NewGuid() };
        _uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);

        var result = await _sut.MarkAsReadAsync(notice.Id, _userId);

        Assert.False(result.Success);
        Assert.Equal("This notice does not belong to you.", result.Error);
        _uow.Notices.Verify(n => n.MarkAsReadAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsRead_OwnNotice_MarksAndSaves()
    {
        var notice = new Notice { Id = Guid.NewGuid(), RecipientId = _userId };
        _uow.Notices.Setup(n => n.GetByIdAsync(notice.Id)).ReturnsAsync(notice);

        var result = await _sut.MarkAsReadAsync(notice.Id, _userId);

        Assert.True(result.Success);
        _uow.Notices.Verify(n => n.MarkAsReadAsync(notice.Id), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUnreadCount_DelegatesToRepository()
    {
        _uow.Notices.Setup(n => n.GetUnreadCountAsync(_userId)).ReturnsAsync(4);

        var result = await _sut.GetUnreadCountAsync(_userId);

        Assert.Equal(4, result);
    }

    [Fact]
    public async Task Create_PersistsUnreadNotice()
    {
        Notice? added = null;
        var senderId = Guid.NewGuid();
        _uow.Notices.Setup(n => n.AddAsync(It.IsAny<Notice>())).Callback<Notice>(n => added = n).Returns(Task.CompletedTask);

        await _sut.CreateAsync(NoticeType.Personal, senderId, _userId, "Title", "Text", "https://url");

        Assert.NotNull(added);
        Assert.False(added.IsRead);
        Assert.Equal(NoticeType.Personal, added.Type);
        Assert.Equal(senderId, added.SenderId);
        Assert.Equal(_userId, added.RecipientId);
        Assert.Equal("Title", added.Title);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByRecipient_ServerNotice_DoesNotLoadSender()
    {
        var notice = new Notice { Id = Guid.NewGuid(), RecipientId = _userId, SenderId = null, Title = "Server" };
        _uow.Notices.Setup(n => n.GetByRecipientAsync(_userId, 1, 20)).ReturnsAsync(new PagedResult<Notice>
        {
            Items = new List<Notice> { notice },
            PageNumber = 1,
            PageSize = 20,
            TotalCount = 1
        });

        var result = await _sut.GetByRecipientAsync(_userId, 1, 20);

        Assert.Single(result.Items);
        _uow.Users.Verify(u => u.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetByRecipient_PersonalNotice_LoadsSender()
    {
        var sender = TestData.NewUser(name: "Sender");
        var notice = new Notice { Id = Guid.NewGuid(), RecipientId = _userId, SenderId = sender.Id, Title = "Hi" };
        _uow.Users.Setup(u => u.GetByIdAsync(sender.Id)).ReturnsAsync(sender);
        _uow.Notices.Setup(n => n.GetByRecipientAsync(_userId, 1, 20)).ReturnsAsync(new PagedResult<Notice>
        {
            Items = new List<Notice> { notice },
            PageNumber = 1,
            PageSize = 20,
            TotalCount = 1
        });

        var result = await _sut.GetByRecipientAsync(_userId, 1, 20);

        Assert.Single(result.Items);
        _uow.Users.Verify(u => u.GetByIdAsync(sender.Id), Times.Once);
    }
}

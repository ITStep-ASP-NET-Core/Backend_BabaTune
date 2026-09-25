using BabaTune.Application.Implementations;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Moq;

namespace BabaTune.Tests.Services;

public class SubscribeServiceTests
{
    private readonly UowMock _uow = new();
    private readonly SubscribeService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public SubscribeServiceTests()
    {
        _sut = new SubscribeService(_uow.Object);
    }

    [Fact]
    public async Task Subscribe_ToSelf_Fails()
    {
        var result = await _sut.SubscribeAsync(_userId, _userId);

        Assert.False(result.Success);
        Assert.Equal("You cannot subscribe to yourself.", result.Error);
        _uow.Subscribes.Verify(s => s.AddAsync(It.IsAny<Subscribe>()), Times.Never);
    }

    [Fact]
    public async Task Subscribe_AlreadySubscribed_Fails()
    {
        var targetId = Guid.NewGuid();
        _uow.Subscribes.Setup(s => s.ExistsAsync(_userId, targetId)).ReturnsAsync(true);

        var result = await _sut.SubscribeAsync(_userId, targetId);

        Assert.False(result.Success);
        Assert.Equal("Already subscribed.", result.Error);
        _uow.Subscribes.Verify(s => s.AddAsync(It.IsAny<Subscribe>()), Times.Never);
    }

    [Fact]
    public async Task Subscribe_NewSubscription_IsPersisted()
    {
        var targetId = Guid.NewGuid();
        Subscribe? added = null;
        _uow.Subscribes
            .Setup(s => s.AddAsync(It.IsAny<Subscribe>()))
            .Callback<Subscribe>(s => added = s)
            .Returns(Task.CompletedTask);

        var result = await _sut.SubscribeAsync(_userId, targetId);

        Assert.True(result.Success);
        Assert.NotNull(added);
        Assert.Equal(_userId, added.SubscriberId);
        Assert.Equal(targetId, added.SubscribedToId);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Unsubscribe_NotSubscribed_Fails()
    {
        var result = await _sut.UnsubscribeAsync(_userId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Subscription not found.", result.Error);
        _uow.Subscribes.Verify(s => s.Delete(It.IsAny<Subscribe>()), Times.Never);
    }

    [Fact]
    public async Task Unsubscribe_Existing_IsDeleted()
    {
        var targetId = Guid.NewGuid();
        var subscribe = new Subscribe { Id = Guid.NewGuid(), SubscriberId = _userId, SubscribedToId = targetId };
        _uow.Subscribes.Setup(s => s.GetAsync(_userId, targetId)).ReturnsAsync(subscribe);

        var result = await _sut.UnsubscribeAsync(_userId, targetId);

        Assert.True(result.Success);
        _uow.Subscribes.Verify(s => s.Delete(subscribe), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSubscriptions_ReturnsTargetUsersAndKeepsPaging()
    {
        var target = TestData.NewUser(name: "Target");
        _uow.Subscribes.Setup(s => s.GetSubscriptionsAsync(_userId, 2, 5)).ReturnsAsync(new PagedResult<Subscribe>
        {
            Items = new List<Subscribe>
            {
                new() { Id = Guid.NewGuid(), SubscriberId = _userId, SubscribedToId = target.Id, SubscribedTo = target }
            },
            PageNumber = 2,
            PageSize = 5,
            TotalCount = 11
        });

        var result = await _sut.GetSubscriptionsAsync(_userId, 2, 5);

        Assert.Single(result.Items);
        Assert.Equal(11, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public async Task GetSubscribers_ReturnsSubscriberUsers()
    {
        var follower = TestData.NewUser(name: "Follower");
        _uow.Subscribes.Setup(s => s.GetSubscribersAsync(_userId, 1, 20)).ReturnsAsync(new PagedResult<Subscribe>
        {
            Items = new List<Subscribe>
            {
                new() { Id = Guid.NewGuid(), SubscriberId = follower.Id, SubscribedToId = _userId, Subscriber = follower }
            },
            PageNumber = 1,
            PageSize = 20,
            TotalCount = 1
        });

        var result = await _sut.GetSubscribersAsync(_userId, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
    }
}

using BabaTune.Application.DTO.Users;
using BabaTune.Application.Implementations;
using BabaTune.Application.Interfaces;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Moq;

namespace BabaTune.Tests.Services;

public class UserServiceTests
{
    private readonly UowMock _uow = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_uow.Object, _hasher.Object);
    }

    private User Add(string? avatarUrl = null)
    {
        var user = TestData.NewUser();
        user.AvatarUrl = avatarUrl;
        _uow.Users.Setup(u => u.GetByIdAsync(user.Id)).ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task GetProfile_Missing_ReturnsNull()
    {
        var result = await _sut.GetProfileAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProfile_Existing_ReadsCountersFromRepositories()
    {
        var user = Add();
        _uow.Subscribes.Setup(s => s.GetSubscriptionsCountAsync(user.Id)).ReturnsAsync(3);
        _uow.Subscribes.Setup(s => s.GetSubscribersCountAsync(user.Id)).ReturnsAsync(7);
        _uow.Songs.Setup(s => s.GetCountByAuthorAsync(user.Id)).ReturnsAsync(5);

        var result = await _sut.GetProfileAsync(user.Id);

        Assert.NotNull(result);
        _uow.Subscribes.Verify(s => s.GetSubscriptionsCountAsync(user.Id), Times.Once);
        _uow.Subscribes.Verify(s => s.GetSubscribersCountAsync(user.Id), Times.Once);
        _uow.Songs.Verify(s => s.GetCountByAuthorAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task UpdateInfo_Missing_Fails()
    {
        var result = await _sut.UpdateInfoAsync(Guid.NewGuid(), new UpdateUserDto { Name = "N" });

        Assert.False(result.Success);
        Assert.Equal("User not found.", result.Error);
    }

    [Fact]
    public async Task UpdateInfo_Existing_ChangesNameAndSaves()
    {
        var user = Add();

        var result = await _sut.UpdateInfoAsync(user.Id, new UpdateUserDto { Name = "New name" });

        Assert.True(result.Success);
        Assert.Equal("New name", user.Name);
        _uow.Users.Verify(u => u.Update(user), Times.Once);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePassword_WrongCurrentPassword_Fails()
    {
        var user = Add();
        _hasher.Setup(h => h.VerifyPassword("wrong", user.PasswordHash)).Returns(false);

        var result = await _sut.UpdatePasswordAsync(user.Id, "wrong", "new");

        Assert.False(result.Success);
        Assert.Equal("Current password is incorrect.", result.Error);
        _hasher.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        _uow.Users.Verify(u => u.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePassword_CorrectCurrentPassword_StoresNewHash()
    {
        var user = Add();
        _hasher.Setup(h => h.VerifyPassword("old", user.PasswordHash)).Returns(true);
        _hasher.Setup(h => h.HashPassword("new")).Returns("new-hash");

        var result = await _sut.UpdatePasswordAsync(user.Id, "old", "new");

        Assert.True(result.Success);
        Assert.Equal("new-hash", user.PasswordHash);
        _uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAvatar_NewFile_DeletesOldAndUploadsNew()
    {
        var user = Add("https://cloud/old.png");
        _uow.Storage
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), "new.png", "image/png", "users/avatars"))
            .ReturnsAsync("https://cloud/new.png");

        var result = await _sut.UpdateAvatarAsync(user.Id, FormFiles.Create("new.png", "image/png"));

        Assert.True(result.Success);
        Assert.Equal("https://cloud/new.png", user.AvatarUrl);
        _uow.Storage.Verify(s => s.DeleteAsync("https://cloud/old.png"), Times.Once);
    }

    [Fact]
    public async Task UpdateAvatar_NullFile_RemovesAvatar()
    {
        var user = Add("https://cloud/old.png");

        var result = await _sut.UpdateAvatarAsync(user.Id, null);

        Assert.True(result.Success);
        Assert.Null(user.AvatarUrl);
        _uow.Storage.Verify(s => s.DeleteAsync("https://cloud/old.png"), Times.Once);
        _uow.Storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAvatar_NoOldAvatarAndNullFile_DoesNotTouchStorage()
    {
        var user = Add();

        var result = await _sut.UpdateAvatarAsync(user.Id, null);

        Assert.True(result.Success);
        _uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}

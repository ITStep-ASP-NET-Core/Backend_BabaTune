using System.Security.Claims;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BabaTune.Tests.Common;

public class UowMock
{
    public Mock<IUnitOfWork> Mock { get; } = new();
    public Mock<IUserRepository> Users { get; } = new();
    public Mock<IRefreshTokenRepository> RefreshTokens { get; } = new();
    public Mock<ISubscribeRepository> Subscribes { get; } = new();
    public Mock<ISongRepository> Songs { get; } = new();
    public Mock<IStorageRepository> Storage { get; } = new();
    public Mock<IAlbumRepository> Albums { get; } = new();
    public Mock<ICategoryRepository> Categories { get; } = new();
    public Mock<IGenreRepository> Genres { get; } = new();
    public Mock<IPlaylistRepository> Playlists { get; } = new();
    public Mock<INoticeRepository> Notices { get; } = new();
    public Mock<IListenHistoryRepository> ListenHistories { get; } = new();

    public IUnitOfWork Object => Mock.Object;

    public UowMock()
    {
        Mock.SetupGet(x => x.Users).Returns(Users.Object);
        Mock.SetupGet(x => x.RefreshTokens).Returns(RefreshTokens.Object);
        Mock.SetupGet(x => x.Subscribes).Returns(Subscribes.Object);
        Mock.SetupGet(x => x.Songs).Returns(Songs.Object);
        Mock.SetupGet(x => x.Storage).Returns(Storage.Object);
        Mock.SetupGet(x => x.Albums).Returns(Albums.Object);
        Mock.SetupGet(x => x.Categories).Returns(Categories.Object);
        Mock.SetupGet(x => x.Genres).Returns(Genres.Object);
        Mock.SetupGet(x => x.Playlists).Returns(Playlists.Object);
        Mock.SetupGet(x => x.Notices).Returns(Notices.Object);
        Mock.SetupGet(x => x.ListenHistories).Returns(ListenHistories.Object);
        Mock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
    }
}

public static class Defaults
{
    public const string SongImage = "https://storage.babatune.app/defaults/song-cover.png";
    public const string PlaylistImage = "https://storage.babatune.app/defaults/playlist-cover.png";
    public const string AlbumImage = "https://storage.babatune.app/defaults/album-cover.png";
}

public static class FormFiles
{
    public static IFormFile Create(string fileName = "file.bin", string contentType = "application/octet-stream", long length = 3)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length);
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(new byte[] { 1, 2, 3 }));
        return file.Object;
    }
}

public static class TestData
{
    public static User NewUser(Guid? id = null, string name = "User") => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        Email = $"{name.ToLower()}@test.com",
        PasswordHash = "hash"
    };

    public static Song NewSong(Guid? id = null, Guid? userId = null, string url = "https://cloud/song.mp3", string imageUrl = Defaults.SongImage)
    {
        var author = NewUser(userId);
        return new Song
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Song",
            Url = url,
            ImageUrl = imageUrl,
            UserId = author.Id,
            User = author
        };
    }
}

public static class ControllerExtensions
{
    public static T WithUser<T>(this T controller, Guid? userId = null) where T : ControllerBase
    {
        var identity = userId is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "test");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        return controller;
    }
}

using BabaTune.Application.DTO.Albums;
using BabaTune.Application.Implementations;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Moq;

namespace BabaTune.Tests.Services;

public class AlbumServiceTests
{
    private readonly UowMock _uow = new();
    private readonly AlbumService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AlbumServiceTests()
    {
        _sut = new AlbumService(_uow.Object);
    }

    private Album Add(Guid? ownerId = null, string image = Defaults.AlbumImage)
    {
        var album = new Album
        {
            Id = Guid.NewGuid(),
            Name = "Album",
            Description = "Description",
            UserId = ownerId ?? _userId,
            ImageUrl = image
        };
        _uow.Albums.Setup(a => a.GetByIdAsync(album.Id)).ReturnsAsync(album);
        return album;
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task Create_WithoutImage_UsesDefaultCover()
    {
        Album? added = null;
        _uow.Albums.Setup(a => a.AddAsync(It.IsAny<Album>())).Callback<Album>(a => added = a).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new CreateAlbumDto(), _userId);

        Assert.True(result.Success);
        Assert.NotNull(added);
        Assert.Equal(_userId, added.UserId);
        Assert.Equal(Defaults.AlbumImage, added.ImageUrl);
        _uow.Storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateInfo_NotAuthor_Fails()
    {
        var album = Add(ownerId: Guid.NewGuid());

        var result = await _sut.UpdateInfoAsync(album.Id, _userId, new UpdateAlbumDto { Name = "X" });

        Assert.False(result.Success);
        Assert.Equal("You are not the author of this album.", result.Error);
        _uow.Albums.Verify(a => a.Update(It.IsAny<Album>()), Times.Never);
    }

    [Fact]
    public async Task UpdateInfo_PartialDto_KeepsUntouchedFields()
    {
        var album = Add();

        var result = await _sut.UpdateInfoAsync(album.Id, _userId, new UpdateAlbumDto { Name = null, Description = "New description" });

        Assert.True(result.Success);
        Assert.Equal("Album", album.Name);
        Assert.Equal("New description", album.Description);
    }

    [Fact]
    public async Task UpdateImage_Reset_DeletesCustomCoverAndRestoresDefault()
    {
        var album = Add(image: "https://cloud/old.png");

        var result = await _sut.UpdateImageAsync(album.Id, _userId, null);

        Assert.True(result.Success);
        Assert.Equal(Defaults.AlbumImage, album.ImageUrl);
        _uow.Storage.Verify(s => s.DeleteAsync("https://cloud/old.png"), Times.Once);
    }

    [Fact]
    public async Task Delete_NotAuthor_Fails()
    {
        var album = Add(ownerId: Guid.NewGuid());

        var result = await _sut.DeleteAsync(album.Id, _userId);

        Assert.False(result.Success);
        _uow.Albums.Verify(a => a.Delete(It.IsAny<Album>()), Times.Never);
    }

    [Fact]
    public async Task Delete_CustomCover_RemovesFileFromStorage()
    {
        var album = Add(image: "https://cloud/c.png");

        var result = await _sut.DeleteAsync(album.Id, _userId);

        Assert.True(result.Success);
        _uow.Storage.Verify(s => s.DeleteAsync("https://cloud/c.png"), Times.Once);
        _uow.Albums.Verify(a => a.Delete(album), Times.Once);
    }

    [Fact]
    public async Task Delete_DefaultCover_DoesNotTouchStorage()
    {
        var album = Add();

        await _sut.DeleteAsync(album.Id, _userId);

        _uow.Storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}

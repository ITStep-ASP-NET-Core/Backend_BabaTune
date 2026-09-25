using BabaTune.Application.DTO.Auth;
using BabaTune.Application.Implementations;
using BabaTune.Application.Interfaces;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Tests.Common;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BabaTune.Tests.Services;

public class AuthServiceTests
{
	private readonly UowMock _uow = new();
	private readonly Mock<IPasswordHasher> _hasher = new();
	private readonly AuthService _sut;

	public AuthServiceTests ( )
	{
		var config = new Mock<IConfiguration>();
		config.Setup(c => c["Jwt:Secret"]).Returns(new string('k', 64));
		config.Setup(c => c["Jwt:Issuer"]).Returns("issuer");
		config.Setup(c => c["Jwt:Audience"]).Returns("audience");

		_sut = new AuthService(_uow.Object, _hasher.Object, config.Object);
	}

	[Fact]
	public async Task Register_EmailAlreadyUsed_Fails ( )
	{
		_uow.Users.Setup(u => u.ExistsByEmailAsync("a@test.com")).ReturnsAsync(true);

		var result = await _sut.RegisterAsync(new RegisterDto { Name = "A", Email = "a@test.com", Password = "pass" });

		Assert.False(result.Success);
		Assert.Equal("Email already in use.", result.Error);
		Assert.Null(result.Data);
		_uow.Users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
	}

	[Fact]
	public async Task Register_NewUser_CreatesUserWithHashedPasswordAndLikedPlaylist ( )
	{
		User? created = null;
		Playlist? liked = null;

		_uow.Users.Setup(u => u.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
		_uow.Users.Setup(u => u.AddAsync(It.IsAny<User>())).Callback<User>(u => created = u).Returns(Task.CompletedTask);
		_uow.Playlists.Setup(p => p.AddAsync(It.IsAny<Playlist>())).Callback<Playlist>(p => liked = p).Returns(Task.CompletedTask);
		_hasher.Setup(h => h.HashPassword("pass")).Returns("hashed");

		var result = await _sut.RegisterAsync(new RegisterDto { Name = "A", Email = "a@test.com", Password = "pass" });

		Assert.True(result.Success);
		Assert.NotNull(result.Data);
		Assert.False(string.IsNullOrEmpty(result.Data.AccessToken));
		Assert.False(string.IsNullOrEmpty(result.Data.RefreshToken));
		Assert.NotNull(created);
		Assert.NotNull(liked);
		Assert.Equal("hashed", created.PasswordHash);
		Assert.Equal("a@test.com", created.Email);
		Assert.False(created.IsChecked);
		Assert.Equal(PlaylistType.Liked, liked.Type);
		Assert.Equal(created.Id, liked.UserId);
		var issuedRefreshToken = result.Data.RefreshToken;
		_uow.RefreshTokens.Verify(r => r.AddAsync(It.Is<RefreshToken>(t =>
			t.UserId == created!.Id && t.Token == issuedRefreshToken)), Times.Once);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task Login_UnknownEmail_Fails ( )
	{
		var result = await _sut.LoginAsync(new LoginDto { Email = "x@test.com", Password = "pass" });

		Assert.False(result.Success);
		Assert.Equal("Invalid email or password.", result.Error);
		_uow.RefreshTokens.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
	}

	[Fact]
	public async Task Login_WrongPassword_FailsWithSameMessage ( )
	{
		var user = TestData.NewUser();
		_uow.Users.Setup(u => u.GetByEmailAsync(user.Email)).ReturnsAsync(user);
		_hasher.Setup(h => h.VerifyPassword("wrong", user.PasswordHash)).Returns(false);

		var result = await _sut.LoginAsync(new LoginDto { Email = user.Email, Password = "wrong" });

		Assert.False(result.Success);
		Assert.Equal("Invalid email or password.", result.Error);
		_uow.RefreshTokens.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
	}

	[Fact]
	public async Task Login_ValidCredentials_StoresRefreshToken ( )
	{
		var user = TestData.NewUser();
		_uow.Users.Setup(u => u.GetByEmailAsync(user.Email)).ReturnsAsync(user);
		_hasher.Setup(h => h.VerifyPassword("pass", user.PasswordHash)).Returns(true);

		var result = await _sut.LoginAsync(new LoginDto { Email = user.Email, Password = "pass" });

		Assert.True(result.Success);
		Assert.NotNull(result.Data);
		Assert.False(string.IsNullOrEmpty(result.Data.AccessToken));
		var issuedToken = result.Data.RefreshToken;
		_uow.RefreshTokens.Verify(r => r.AddAsync(It.Is<RefreshToken>(t =>
			t.UserId == user.Id
			&& t.Token == issuedToken
			&& t.ExpiresAt > DateTime.UtcNow.AddDays(6))), Times.Once);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task Refresh_UnknownToken_Fails ( )
	{
		var result = await _sut.RefreshAsync("missing");

		Assert.False(result.Success);
		Assert.Equal("Invalid or expired refresh token.", result.Error);
	}

	[Theory]
	[InlineData(true, 1)]
	[InlineData(false, -1)]
	public async Task Refresh_RevokedOrExpiredToken_FailsWithoutRevokingAgain ( bool revoked, int expiresInDays )
	{
		var token = new RefreshToken
		{
			Id = Guid.NewGuid(),
			UserId = Guid.NewGuid(),
			Token = "t",
			ExpiresAt = DateTime.UtcNow.AddDays(expiresInDays),
			RevokedAt = revoked ? DateTime.UtcNow : null
		};
		_uow.RefreshTokens.Setup(r => r.GetByTokenAsync("t")).ReturnsAsync(token);

		var result = await _sut.RefreshAsync("t");

		Assert.False(result.Success);
		_uow.RefreshTokens.Verify(r => r.RevokeAsync(It.IsAny<Guid>()), Times.Never);
		_uow.RefreshTokens.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
	}

	[Fact]
	public async Task Refresh_ValidToken_RevokesOldAndIssuesNew ( )
	{
		var user = TestData.NewUser();
		var old = new RefreshToken
		{
			Id = Guid.NewGuid(),
			UserId = user.Id,
			Token = "old",
			ExpiresAt = DateTime.UtcNow.AddDays(1)
		};
		_uow.RefreshTokens.Setup(r => r.GetByTokenAsync("old")).ReturnsAsync(old);
		_uow.Users.Setup(u => u.GetByIdAsync(user.Id)).ReturnsAsync(user);

		var result = await _sut.RefreshAsync("old");

		Assert.True(result.Success);
		Assert.NotNull(result.Data);
		Assert.NotEqual("old", result.Data.RefreshToken);
		_uow.RefreshTokens.Verify(r => r.RevokeAsync(old.Id), Times.Once);
		_uow.RefreshTokens.Verify(r => r.AddAsync(It.Is<RefreshToken>(t =>
			t.UserId == user.Id && t.Token != "old")), Times.Once);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}

	[Fact]
	public async Task Refresh_UserDeleted_Fails ( )
	{
		var old = new RefreshToken
		{
			Id = Guid.NewGuid(),
			UserId = Guid.NewGuid(),
			Token = "old",
			ExpiresAt = DateTime.UtcNow.AddDays(1)
		};
		_uow.RefreshTokens.Setup(r => r.GetByTokenAsync("old")).ReturnsAsync(old);

		var result = await _sut.RefreshAsync("old");

		Assert.False(result.Success);
		_uow.RefreshTokens.Verify(r => r.RevokeAsync(It.IsAny<Guid>()), Times.Never);
		_uow.RefreshTokens.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
	}

	[Fact]
	public async Task Logout_UnknownToken_DoesNothing ( )
	{
		await _sut.LogoutAsync("missing");

		_uow.RefreshTokens.Verify(r => r.RevokeAsync(It.IsAny<Guid>()), Times.Never);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Never);
	}

	[Fact]
	public async Task Logout_KnownToken_RevokesAndSaves ( )
	{
		var token = new RefreshToken { Id = Guid.NewGuid(), Token = "t" };
		_uow.RefreshTokens.Setup(r => r.GetByTokenAsync("t")).ReturnsAsync(token);

		await _sut.LogoutAsync("t");

		_uow.RefreshTokens.Verify(r => r.RevokeAsync(token.Id), Times.Once);
		_uow.Mock.Verify(u => u.SaveChangesAsync(), Times.Once);
	}
}
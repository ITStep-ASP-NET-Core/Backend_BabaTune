using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BabaTune.Application.Common;
using BabaTune.Application.DTO.Auth;
using BabaTune.Application.DTO.Users;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BabaTune.Application.Implementations
{
	public class AuthService : IAuthService
	{
		private readonly IUnitOfWork _uow;
		private readonly IPasswordHasher _passwordHasher;
		private readonly IConfiguration _configuration;

		public AuthService ( IUnitOfWork uow, IPasswordHasher passwordHasher, IConfiguration configuration )
		{
			_uow = uow;
			_passwordHasher = passwordHasher;
			_configuration = configuration;
		}

		public async Task<Result<AuthResponseDto>> RegisterAsync ( RegisterDto dto )
		{
			if(await _uow.Users.ExistsByEmailAsync(dto.Email))
				return Result<AuthResponseDto>.Fail("Email already in use.");

			var user = new User
			{
				Id = Guid.NewGuid(),
				Name = dto.Name,
				Email = dto.Email,
				PasswordHash = _passwordHasher.HashPassword(dto.Password),
				IsChecked = false,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			await _uow.Users.AddAsync(user);

			var likedPlaylist = new Playlist
			{
				Id = Guid.NewGuid(),
				Type = PlaylistType.Liked,
				UserId = user.Id,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			await _uow.Playlists.AddAsync(likedPlaylist);

			var response = await GenerateAuthResponseAsync(user);
			await _uow.SaveChangesAsync();

			return Result.Ok(response);
		}

		public async Task<Result<AuthResponseDto>> LoginAsync ( LoginDto dto )
		{
			var user = await _uow.Users.GetByEmailAsync(dto.Email);
			if(user is null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
				return Result<AuthResponseDto>.Fail("Invalid email or password.");

			var response = await GenerateAuthResponseAsync(user);
			await _uow.SaveChangesAsync();

			return Result.Ok(response);
		}

		public async Task<Result<AuthResponseDto>> RefreshAsync ( string refreshToken )
		{
			var token = await _uow.RefreshTokens.GetByTokenAsync(refreshToken);
			if(token is null || token.RevokedAt is not null || token.ExpiresAt < DateTime.UtcNow)
				return Result<AuthResponseDto>.Fail("Invalid or expired refresh token.");

			var user = await _uow.Users.GetByIdAsync(token.UserId);
			if(user is null)
				return Result<AuthResponseDto>.Fail("Invalid or expired refresh token.");

			await _uow.RefreshTokens.RevokeAsync(token.Id);

			var response = await GenerateAuthResponseAsync(user);
			await _uow.SaveChangesAsync();

			return Result.Ok(response);
		}

		public async Task LogoutAsync ( string refreshToken )
		{
			var token = await _uow.RefreshTokens.GetByTokenAsync(refreshToken);
			if(token is null)
				return;

			await _uow.RefreshTokens.RevokeAsync(token.Id);
			await _uow.SaveChangesAsync();
		}

		private async Task<AuthResponseDto> GenerateAuthResponseAsync ( User user )
		{
			var accessToken = GenerateAccessToken(user);
			var expiresAt = DateTime.UtcNow.AddMinutes(15);
			var refreshToken = await CreateRefreshTokenAsync(user.Id);
			var userDto = await BuildUserDtoAsync(user);

			return AuthMapper.ToDto(accessToken, refreshToken, expiresAt, userDto);
		}

		private async Task<UserDto> BuildUserDtoAsync ( User user )
		{
			var subscriptions = await _uow.Subscribes.GetSubscriptionsCountAsync(user.Id);
			var tracks = await _uow.Songs.GetCountByAuthorAsync(user.Id);
			var listenersCount = await _uow.Subscribes.GetSubscribersCountAsync(user.Id);

			return UserMapper.ToDto(user, subscriptions, tracks, listenersCount);
		}

		private string GenerateAccessToken ( User user )
		{
			var key = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));

			var claims = new[]
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Name, user.Name)
			};

			var token = new JwtSecurityToken(
				issuer: _configuration["Jwt:Issuer"],
				audience: _configuration["Jwt:Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(15),
				signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		private async Task<string> CreateRefreshTokenAsync ( Guid userId )
		{
			var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

			var refreshToken = new RefreshToken
			{
				Id = Guid.NewGuid(),
				Token = tokenValue,
				UserId = userId,
				ExpiresAt = DateTime.UtcNow.AddDays(7),
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			await _uow.RefreshTokens.AddAsync(refreshToken);

			return tokenValue;
		}
	}
}
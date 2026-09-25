using BabaTune.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Data
{
	public class ApplicationContext : DbContext
	{
		public ApplicationContext ( DbContextOptions<ApplicationContext> options ) : base(options) { }

		public DbSet<User> Users { get; set; }
		public DbSet<RefreshToken> RefreshTokens { get; set; }
		public DbSet<Subscribe> Subscribes { get; set; }
		public DbSet<Song> Songs { get; set; }
		public DbSet<Album> Albums { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<Genre> Genres { get; set; }
		public DbSet<Playlist> Playlists { get; set; }
		public DbSet<PlaylistItem> PlaylistItems { get; set; }
		public DbSet<Notice> Notices { get; set; }
		public DbSet<ListenHistory> ListenHistories { get; set; }

		protected override void OnModelCreating ( ModelBuilder modelBuilder )
		{
			modelBuilder.Entity<User>(entity =>
			{
				entity.HasKey(u => u.Id);
				entity.HasIndex(u => u.Email).IsUnique();
			});

			modelBuilder.Entity<RefreshToken>(entity =>
			{
				entity.HasKey(rt => rt.Id);

				entity.HasOne(rt => rt.User)
					  .WithMany(u => u.RefreshTokens)
					  .HasForeignKey(rt => rt.UserId)
					  .OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<Subscribe>(entity =>
			{
				entity.HasKey(s => s.Id);
				entity.HasIndex(s => new { s.SubscriberId, s.SubscribedToId }).IsUnique();

				entity.HasOne(s => s.Subscriber)
					  .WithMany(u => u.Subscriptions)
					  .HasForeignKey(s => s.SubscriberId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(s => s.SubscribedTo)
					  .WithMany(u => u.Subscribers)
					  .HasForeignKey(s => s.SubscribedToId)
					  .OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<Song>(entity =>
			{
				entity.HasKey(s => s.Id);

				entity.HasOne(s => s.Album)
					  .WithMany(a => a.Songs)
					  .HasForeignKey(s => s.AlbumId)
					  .OnDelete(DeleteBehavior.SetNull);

				entity.HasOne(s => s.User)
					  .WithMany(u => u.Songs)
					  .HasForeignKey(s => s.UserId)
					  .OnDelete(DeleteBehavior.SetNull);

				entity.HasMany(s => s.Categories)
					  .WithMany(c => c.Songs);

				entity.HasMany(s => s.Genres)
					  .WithMany(g => g.Songs);
			});

			modelBuilder.Entity<Album>(entity =>
			{
				entity.HasKey(a => a.Id);

				entity.HasOne(a => a.User)
					  .WithMany(u => u.Albums)
					  .HasForeignKey(a => a.UserId)
					  .OnDelete(DeleteBehavior.SetNull);
			});

			modelBuilder.Entity<Category>(entity => entity.HasKey(c => c.Id));

			modelBuilder.Entity<Genre>(entity => entity.HasKey(g => g.Id));

			modelBuilder.Entity<Playlist>(entity =>
			{
				entity.HasKey(p => p.Id);

				entity.HasOne(p => p.User)
					  .WithMany(u => u.Playlists)
					  .HasForeignKey(p => p.UserId)
					  .OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<PlaylistItem>(entity =>
			{
				entity.HasKey(pi => pi.Id);
				entity.HasIndex(pi => new { pi.PlaylistId, pi.SongId }).IsUnique();

				entity.HasOne(pi => pi.Playlist)
					  .WithMany(p => p.Items)
					  .HasForeignKey(pi => pi.PlaylistId)
					  .OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(pi => pi.Song)
					  .WithMany()
					  .HasForeignKey(pi => pi.SongId)
					  .OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<Notice>(entity =>
			{
				entity.HasKey(n => n.Id);

				entity.HasOne(n => n.Sender)
					  .WithMany()
					  .HasForeignKey(n => n.SenderId)
					  .OnDelete(DeleteBehavior.SetNull);

				entity.HasOne(n => n.Recipient)
					  .WithMany()
					  .HasForeignKey(n => n.RecipientId)
					  .OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<ListenHistory>(entity =>
			{
				entity.HasKey(lh => lh.Id);
				entity.HasIndex(lh => new { lh.UserId, lh.SongId }).IsUnique();

				entity.HasOne(lh => lh.User)
					  .WithMany(u => u.ListenHistory)
					  .HasForeignKey(lh => lh.UserId)
					  .OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(lh => lh.Song)
					  .WithMany()
					  .HasForeignKey(lh => lh.SongId)
					  .OnDelete(DeleteBehavior.Cascade);
			});
		}
	}
}


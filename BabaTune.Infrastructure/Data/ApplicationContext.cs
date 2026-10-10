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
				entity.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt });

				entity.HasDiscriminator<NoticeType>("Type")
					  .HasValue<FriendRequestNotice>(NoticeType.FriendRequest)
					  .HasValue<MessageNotice>(NoticeType.Message)
					  .HasValue<NewSongNotice>(NoticeType.NewSong)
					  .HasValue<NewAlbumNotice>(NoticeType.NewAlbum)
					  .HasValue<NewRoomNotice>(NoticeType.NewRoom)
					  .HasValue<NewSubscriptionNotice>(NoticeType.NewSubscription)
					  .HasValue<ServerNotice>(NoticeType.Server);

				entity.HasOne(n => n.Recipient)
					  .WithMany()
					  .HasForeignKey(n => n.RecipientId)
					  .OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<FriendRequestNotice>(entity =>
			{
				entity.HasIndex(n => new { n.FriendShipId }).IsUnique();

				entity.HasOne(n => n.Sender)
					  .WithMany()
					  .HasForeignKey(n => n.SenderId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(n => n.FriendShip)
					.WithMany()
					.HasForeignKey(n => n.FriendShipId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<MessageNotice>(entity =>
			{
				entity.HasOne(n => n.Chat)
					  .WithMany()
					  .HasForeignKey(n => n.ChatId)
					  .OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<NewSongNotice>(entity =>
			{
				entity.HasOne(n => n.Song)
					  .WithMany()
					  .HasForeignKey(n => n.SongId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(n => n.Author)
					  .WithMany()
					  .HasForeignKey(n => n.AuthorId)
					  .OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<NewAlbumNotice>(entity =>
			{
				entity.HasOne(n => n.Album)
					  .WithMany()
					  .HasForeignKey(n => n.AlbumId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(n => n.Author)
					  .WithMany()
					  .HasForeignKey(n => n.AuthorId)
					  .OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<NewRoomNotice>(entity =>
			{
				entity.HasOne(n => n.Owner)
					  .WithMany()
					  .HasForeignKey(n => n.OwnerId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(n => n.Room)
					  .WithMany()
					  .HasForeignKey(n => n.RoomId)
					  .OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<NewSubscriptionNotice>(entity =>
			{
				entity.HasOne(n => n.Subscribe)
					  .WithMany()
					  .HasForeignKey(n => n.SubscribeId)
					  .OnDelete(DeleteBehavior.Cascade);
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

			modelBuilder.Entity<Friendship>(entity =>
			{
				entity.HasKey(f => f.Id);
				entity.HasIndex(f => new { f.SenderId, f.RecipientId }).IsUnique();
				entity.HasIndex(f => f.RecipientId);

				entity.ToTable(t => t.HasCheckConstraint(
					"CK_Friendship_NotSelf", "[SenderId] <> [RecipientId]"));

				entity.HasOne(f => f.Sender)
					  .WithMany()
					  .HasForeignKey(f => f.SenderId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(f => f.Recipient)
					  .WithMany()
					  .HasForeignKey(f => f.RecipientId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(f => f.Chat)
					  .WithOne()
					  .HasForeignKey<Friendship>(f => f.ChatId)
					  .OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<Room>(entity =>
			{
				entity.HasKey(r => r.Id);
				entity.HasIndex(r => r.OwnerId);

				entity.HasOne(r => r.Owner)
					  .WithOne()
					  .HasForeignKey<Room>(r => r.OwnerId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(r => r.Chat)
					  .WithOne()
					  .HasForeignKey<Room>(r => r.ChatId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(r => r.CurrentSong)
					  .WithMany()
					  .HasForeignKey(r => r.CurrentSongId)
					  .OnDelete(DeleteBehavior.ClientSetNull);

				entity.HasMany(r => r.Members)
					  .WithOne(u => u.Room)
					  .HasForeignKey(u => u.RoomId)
					  .OnDelete(DeleteBehavior.SetNull);
			});

			modelBuilder.Entity<Chat>(entity =>
			{
				entity.HasKey(c => c.Id);
			});

			modelBuilder.Entity<QueueItem>(entity =>
			{
				entity.HasKey(q => q.Id);
				entity.HasIndex(q => new { q.RoomId, q.AddedAt });

				entity.HasOne(q => q.Room)
					  .WithMany(r => r.Queue)
					  .HasForeignKey(q => q.RoomId)
					  .OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(q => q.Song)
					  .WithMany()
					  .HasForeignKey(q => q.SongId)
					  .OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<Message>(entity =>
			{
				entity.HasKey(m => m.Id);

				entity.HasDiscriminator<MessageType>("Type")
					  .HasValue<TextMessage>(MessageType.Text)
					  .HasValue<SongMessage>(MessageType.Song);

				entity.HasOne(m => m.Chat)
					  .WithMany(c => c.Messages)
					  .HasForeignKey(m => m.ChatId)
					  .OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(m => m.User)
					  .WithMany()
					  .HasForeignKey(m => m.UserId)
					  .OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<SongMessage>(entity =>
			{
				entity.HasOne(m => m.Song)
					.WithMany()
					.HasForeignKey(m => m.SongId)
					.OnDelete(DeleteBehavior.Restrict);
			});

		}
	}
}
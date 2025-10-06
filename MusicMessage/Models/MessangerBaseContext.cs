using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MusicMessage.Models;

public partial class MessangerBaseContext : DbContext
{
    public MessangerBaseContext()
    {
    }

    public MessangerBaseContext(DbContextOptions<MessangerBaseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Message> Messages { get; set; }
	public virtual DbSet<Reaction> Reactions { get; set; }
	public virtual DbSet<ChatPreview> ChatPreviews { get; set; }
	public virtual DbSet<User> Users { get; set; }
	public virtual DbSet<Friendship> Friendships { get; set; }
	public virtual DbSet<Post> Posts { get; set; }
	public virtual DbSet<PostLike> PostLikes { get; set; }
	public virtual DbSet<CommentLike> CommentLikes { get; set; }
	public virtual DbSet<PostComment> PostComments { get; set; }
	public virtual DbSet<PostShare> PostShares { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost; Database=MessangerBase; Trusted_Connection=True; MultipleActiveResultSets=true; TrustServerCertificate=true;encrypt=false");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("Message");

            entity.Property(e => e.AudioPath).HasMaxLength(50);
            entity.Property(e => e.MessageType)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.StickerId).HasMaxLength(50);
            entity.Property(e => e.Timestamp).HasColumnType("datetime");

            entity.HasOne(d => d.Receiver).WithMany(p => p.MessageReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Message_User1");

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Message_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.UserName).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
		modelBuilder.Entity<Reaction>(entity =>
		{
			entity.ToTable("Reactions");

			entity.HasOne(d => d.Message)
				.WithMany(p => p.MessageReactions)
				.HasForeignKey(d => d.MessageId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
				.WithMany()
				.HasForeignKey(d => d.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		modelBuilder.Entity<ChatPreview>(entity =>
		{
			entity.ToTable("ChatPreviews");
			entity.HasKey(e => e.ChatPreviewId);

			entity.Property(e => e.OtherUserName).HasMaxLength(50);
			entity.Property(e => e.LastMessage).HasMaxLength(500);
			entity.Property(e => e.LastMessageTime).HasColumnType("datetime2");
			entity.Property(e => e.UnreadCount).HasDefaultValue(0);

			entity.HasOne(e => e.User)
				.WithMany()
				.HasForeignKey(e => e.UserId)
				.OnDelete(DeleteBehavior.NoAction); 

			entity.HasOne(e => e.OtherUser)
				.WithMany()
				.HasForeignKey(e => e.OtherUserId)
				.OnDelete(DeleteBehavior.NoAction); 

			entity.HasIndex(e => new { e.UserId, e.OtherUserId })
				.IsUnique();
		}); modelBuilder.Entity<ChatPreview>(entity =>
		{
			entity.ToTable("ChatPreviews");
			entity.HasKey(e => e.ChatPreviewId);

			entity.Property(e => e.OtherUserName).HasMaxLength(50);
			entity.Property(e => e.LastMessage).HasMaxLength(500);
			entity.Property(e => e.LastMessageTime).HasColumnType("datetime2");
			entity.Property(e => e.UnreadCount).HasDefaultValue(0);

			entity.HasOne(e => e.User)
				.WithMany()
				.HasForeignKey(e => e.UserId)
				.OnDelete(DeleteBehavior.NoAction); 

			entity.HasOne(e => e.OtherUser)
				.WithMany()
				.HasForeignKey(e => e.OtherUserId)
				.OnDelete(DeleteBehavior.NoAction); 

			entity.HasIndex(e => new { e.UserId, e.OtherUserId })
				.IsUnique();
		});
		modelBuilder.Entity<Friendship>(entity =>
		{
			entity.ToTable("Friendships");
			entity.HasKey(e => e.FriendshipId);

			entity.HasOne(d => d.Requester)
				.WithMany()
				.HasForeignKey(d => d.RequesterId)
				.OnDelete(DeleteBehavior.NoAction);

			entity.HasOne(d => d.Addressee)
				.WithMany()
				.HasForeignKey(d => d.AddresseeId)
				.OnDelete(DeleteBehavior.NoAction);

			entity.HasIndex(e => new { e.RequesterId, e.AddresseeId })
				.IsUnique();
		});
		modelBuilder.Entity<Post>(entity =>
		{
			entity.ToTable("Posts");

			entity.Property(e => e.Content).HasMaxLength(1000);
			entity.Property(e => e.ImagePath).HasMaxLength(500);
			entity.Property(e => e.VideoPath).HasMaxLength(500);
			entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
			entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");

			entity.HasOne(d => d.Author)
				.WithMany()
				.HasForeignKey(d => d.AuthorId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<PostLike>(entity =>
		{
			entity.ToTable("PostLikes");

			entity.HasIndex(e => new { e.PostId, e.UserId }).IsUnique();

			entity.HasOne(d => d.Post)
				.WithMany(p => p.Likes)
				.HasForeignKey(d => d.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
				.WithMany()
				.HasForeignKey(d => d.UserId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<PostComment>(entity =>
		{
			entity.ToTable("PostComments");

			entity.Property(e => e.Content).HasMaxLength(500);
			entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
			entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");

			entity.HasOne(d => d.Post)
				.WithMany(p => p.Comments)
				.HasForeignKey(d => d.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Author)
				.WithMany()
				.HasForeignKey(d => d.AuthorId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(d => d.ParentComment)
				.WithMany(p => p.Replies)
				.HasForeignKey(d => d.ParentCommentId)
				.OnDelete(DeleteBehavior.Restrict);
		});
		modelBuilder.Entity<CommentLike>(entity =>
		{
			entity.ToTable("CommentLikes");

			entity.HasIndex(e => new { e.CommentId, e.UserId }).IsUnique();

			entity.HasOne(d => d.Comment)
				.WithMany(p => p.Likes)
				.HasForeignKey(d => d.CommentId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
				.WithMany()
				.HasForeignKey(d => d.UserId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<PostShare>(entity =>
		{
			entity.ToTable("PostShares");

			entity.Property(e => e.SharedComment).HasMaxLength(500);
			entity.Property(e => e.SharedAt).HasColumnType("datetime2");

			entity.HasOne(d => d.Post)
				.WithMany(p => p.Shares)
				.HasForeignKey(d => d.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
				.WithMany()
				.HasForeignKey(d => d.UserId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		OnModelCreatingPartial(modelBuilder);
	}

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

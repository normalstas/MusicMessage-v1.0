using Microsoft.EntityFrameworkCore;
using MusicMessage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicMessage.Repository
{
	public interface IPostRepository
	{
		Task<Post> CreatePostAsync(int authorId, string content, string imagePath = null, string videoPath = null, bool isPublic = true);
		Task<List<Post>> GetNewsFeedAsync(int userId, int page = 1, int pageSize = 20);
		Task<List<Post>> GetUserPostsAsync(int userId);
		Task<Post> GetPostByIdAsync(int postId);
		Task<bool> LikePostAsync(int postId, int userId);
		Task<bool> UnlikePostAsync(int postId, int userId);
		Task<PostComment> AddCommentAsync(int postId, int authorId, string content, int? parentCommentId = null);
		Task<bool> DeletePostAsync(int postId, int authorId);
		Task<List<PostComment>> GetPostCommentsAsync(int postId);
		Task<bool> IsPostLikedByUserAsync(int postId, int userId);
		Task<int> GetPostLikesCountAsync(int postId);
		Task<int> GetPostCommentsCountAsync(int postId);
		Task<bool> LikeCommentAsync(int commentId, int userId);
		Task<bool> UnlikeCommentAsync(int commentId, int userId);
		Task<bool> IsCommentLikedByUserAsync(int commentId, int userId);
		Task<int> GetCommentLikesCountAsync(int commentId);

	}

	public class PostRepository : IPostRepository
	{
		private readonly IDbContextFactory<MessangerBaseContext> _contextFactory;

		public PostRepository(IDbContextFactory<MessangerBaseContext> contextFactory)
		{
			_contextFactory = contextFactory;
		}

		public async Task<Post> CreatePostAsync(int authorId, string content, string imagePath = null, string videoPath = null, bool isPublic = true)
		{
			using var context = _contextFactory.CreateDbContext();

			var post = new Post
			{
				AuthorId = authorId,
				Content = content,
				ImagePath = imagePath,
				VideoPath = videoPath,
				IsPublic = isPublic, 
				CreatedAt = DateTime.Now
			};

			context.Posts.Add(post);
			await context.SaveChangesAsync();

			return await context.Posts
				.Include(p => p.Author)
				.FirstOrDefaultAsync(p => p.PostId == post.PostId);
		}

		public async Task<List<Post>> GetNewsFeedAsync(int userId, int page = 1, int pageSize = 20)
		{
			using var context = _contextFactory.CreateDbContext();

			var friendIds = await context.Friendships
				.Where(f => (f.RequesterId == userId || f.AddresseeId == userId) &&
						   f.Status == FriendshipStatus.Accepted)
				.Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
				.ToListAsync();

			friendIds.Add(userId); 

			return await context.Posts
				.Include(p => p.Author)
				.Include(p => p.Likes)
				.Include(p => p.Comments)
				.Where(p => friendIds.Contains(p.AuthorId) && p.IsPublic) 
				.OrderByDescending(p => p.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<Post>> GetUserPostsAsync(int userId)
		{
			using var context = _contextFactory.CreateDbContext();

			return await context.Posts
				.Include(p => p.Author)
				.Include(p => p.Likes)
				.Include(p => p.Comments)
				.Where(p => p.AuthorId == userId && p.IsPublic)
				.OrderByDescending(p => p.CreatedAt)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Post> GetPostByIdAsync(int postId)
		{
			using var context = _contextFactory.CreateDbContext();

			return await context.Posts
				.Include(p => p.Author)
				.Include(p => p.Likes)
				.Include(p => p.Comments)
					.ThenInclude(c => c.Author)
				.FirstOrDefaultAsync(p => p.PostId == postId);
		}

		public async Task<bool> LikePostAsync(int postId, int userId)
		{
			using var context = _contextFactory.CreateDbContext();

			var existingLike = await context.PostLikes
				.FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == userId);

			if (existingLike != null) return false;

			var like = new PostLike
			{
				PostId = postId,
				UserId = userId,
				LikedAt = DateTime.UtcNow
			};

			context.PostLikes.Add(like);


			var post = await context.Posts.FindAsync(postId);
			if (post != null)
			{
				post.LikesCount++;
			}

			await context.SaveChangesAsync();
			return true;
		}
		public async Task<bool> LikeCommentAsync(int commentId, int userId)
		{
			using var context = _contextFactory.CreateDbContext();

			var existingLike = await context.CommentLikes
				.FirstOrDefaultAsync(cl => cl.CommentId == commentId && cl.UserId == userId);

			if (existingLike != null) return false;

			var like = new CommentLike
			{
				CommentId = commentId,
				UserId = userId,
				LikedAt = DateTime.UtcNow
			};

			context.CommentLikes.Add(like);

			var comment = await context.PostComments.FindAsync(commentId);
			if (comment != null)
			{
				comment.LikesCount++;
			}

			await context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> UnlikeCommentAsync(int commentId, int userId)
		{
			using var context = _contextFactory.CreateDbContext();

			var like = await context.CommentLikes
				.FirstOrDefaultAsync(cl => cl.CommentId == commentId && cl.UserId == userId);

			if (like == null) return false;

			context.CommentLikes.Remove(like);

			var comment = await context.PostComments.FindAsync(commentId);
			if (comment != null && comment.LikesCount > 0)
			{
				comment.LikesCount--;
			}

			await context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> IsCommentLikedByUserAsync(int commentId, int userId)
		{
			using var context = _contextFactory.CreateDbContext();

			return await context.CommentLikes
				.AnyAsync(cl => cl.CommentId == commentId && cl.UserId == userId);
		}

		public async Task<int> GetCommentLikesCountAsync(int commentId)
		{
			using var context = _contextFactory.CreateDbContext();

			return await context.CommentLikes
				.CountAsync(cl => cl.CommentId == commentId);
		}
		public async Task<bool> UnlikePostAsync(int postId, int userId)
		{
			using var context = _contextFactory.CreateDbContext();

			var like = await context.PostLikes
				.FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == userId);

			if (like == null) return false;

			context.PostLikes.Remove(like);


			var post = await context.Posts.FindAsync(postId);
			if (post != null && post.LikesCount > 0)
			{
				post.LikesCount--;
			}

			await context.SaveChangesAsync();
			return true;
		}

		public async Task<PostComment> AddCommentAsync(int postId, int authorId, string content, int? parentCommentId = null)
		{
			using var context = _contextFactory.CreateDbContext();

			var comment = new PostComment
			{
				PostId = postId,
				AuthorId = authorId,
				Content = content,
				ParentCommentId = parentCommentId,
				CreatedAt = DateTime.Now 
			};

			context.PostComments.Add(comment);

			var post = await context.Posts.FindAsync(postId);
			if (post != null)
			{
				post.CommentsCount++;
			}

			await context.SaveChangesAsync();

			return await context.PostComments
				.Include(c => c.Author)
				.FirstOrDefaultAsync(c => c.CommentId == comment.CommentId);
		}

		public async Task<bool> DeletePostAsync(int postId, int authorId)
		{
			using var context = _contextFactory.CreateDbContext();

			using var transaction = await context.Database.BeginTransactionAsync();

			try
			{
				var post = await context.Posts
					.FirstOrDefaultAsync(p => p.PostId == postId && p.AuthorId == authorId);

				if (post == null) return false;

				var likes = await context.PostLikes.Where(pl => pl.PostId == postId).ToListAsync();
				context.PostLikes.RemoveRange(likes);

				var shares = await context.PostShares.Where(ps => ps.PostId == postId).ToListAsync();
				context.PostShares.RemoveRange(shares);

				var comments = await context.PostComments.Where(pc => pc.PostId == postId).ToListAsync();
				context.PostComments.RemoveRange(comments);

				context.Posts.Remove(post);

				await context.SaveChangesAsync();
				await transaction.CommitAsync();

				return true;
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
		}

		public async Task<List<PostComment>> GetPostCommentsAsync(int postId)
		{
			using var context = _contextFactory.CreateDbContext();

			return await context.PostComments
				.Include(c => c.Author)
				.Include(c => c.Replies)
					.ThenInclude(r => r.Author)
				.Where(c => c.PostId == postId && c.ParentCommentId == null)
				.OrderBy(c => c.CreatedAt) 
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<bool> IsPostLikedByUserAsync(int postId, int userId)
		{
			using var context = _contextFactory.CreateDbContext();

			return await context.PostLikes
				.AnyAsync(pl => pl.PostId == postId && pl.UserId == userId);
		}

		public async Task<int> GetPostLikesCountAsync(int postId)
		{
			using var context = _contextFactory.CreateDbContext();

			return await context.PostLikes
				.CountAsync(pl => pl.PostId == postId);
		}

		public async Task<int> GetPostCommentsCountAsync(int postId)
		{
			using var context = _contextFactory.CreateDbContext();

			return await context.PostComments
				.CountAsync(pc => pc.PostId == postId);
		}
	}
}

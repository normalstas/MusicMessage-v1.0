using Microsoft.EntityFrameworkCore;
using MusicMessage.Models;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicMessage.Repository
{
	public interface IFriendsRepository
	{
		Task<List<UserSearchResult>> SearchUsersAsync(int currentUserId, string searchTerm);
		Task SendFriendRequestAsync(int requesterId, int addresseeId);
		Task AcceptFriendRequestAsync(int friendshipId);
		Task RejectFriendRequestAsync(int friendshipId);
		Task RemoveFriendAsync(int friendshipId);
		Task BlockUserAsync(int currentUserId, int targetUserId);
		Task UnblockUserAsync(int friendshipId);
		Task<List<Friendship>> GetFriendRequestsAsync(int userId);
		Task<List<User>> GetFriendsAsync(int userId);
		Task<List<User>> GetOnlineFriendsAsync(int userId);
		Task<Friendship> GetFriendshipStatusAsync(int user1Id, int user2Id);
		Task CancelFriendRequestAsync(int friendshipId);
		Task<ChatPreview> GetOrCreateChatAsync(int userId1, int userId2);
	}

	public class FriendsRepository : IFriendsRepository
	{
		private readonly IDbContextFactory<MessangerBaseContext> _contextFactory;

		public FriendsRepository(IDbContextFactory<MessangerBaseContext> contextFactory)
		{
			_contextFactory = contextFactory;
		}
		
		public async Task<List<UserSearchResult>> SearchUsersAsync(int currentUserId, string searchTerm)
		{
			using var context = _contextFactory.CreateDbContext();

			var query = context.Users
				.Where(u => u.UserId != currentUserId &&
						   (u.UserName.Contains(searchTerm) || u.Email.Contains(searchTerm)))
				.Select(u => new UserSearchResult
				{
					UserId = u.UserId,
					UserName = u.UserName,
					Email = u.Email,
					AvatarPath = u.AvatarPath,
					IsOnline = u.IsOnline,
					LastSeen = u.LastSeen,
					IsCurrentUser = false
				});

			var results = await query.ToListAsync();

			// Получаем статусы дружбы для каждого пользователя
			foreach (var user in results)
			{
				var friendship = await GetFriendshipStatusAsync(currentUserId, user.UserId);
				user.FriendshipStatus = friendship?.Status;
			}

			return results;
		}

		public async Task SendFriendRequestAsync(int requesterId, int addresseeId)
		{
			using var context = _contextFactory.CreateDbContext();

			// Проверяем, нет ли уже существующей заявки
			var existingFriendship = await context.Friendships
				.FirstOrDefaultAsync(f =>
					(f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
					(f.RequesterId == addresseeId && f.AddresseeId == requesterId));

			if (existingFriendship != null)
			{
				throw new InvalidOperationException("Заявка в друзья уже существует");
			}

			var friendship = new Friendship
			{
				RequesterId = requesterId,
				AddresseeId = addresseeId,
				Status = FriendshipStatus.Pending,
				CreatedAt = DateTime.UtcNow
			};

			context.Friendships.Add(friendship);
			await context.SaveChangesAsync();
		}
		public async Task<ChatPreview> GetOrCreateChatAsync(int userId1, int userId2)
		{
			using var context = _contextFactory.CreateDbContext();

			// Пытаемся найти существующий чат
			var existingChat = await context.ChatPreviews
				.FirstOrDefaultAsync(c =>
					(c.UserId == userId1 && c.OtherUserId == userId2) ||
					(c.UserId == userId2 && c.OtherUserId == userId1));

			if (existingChat != null)
			{
				return existingChat;
			}

			// Если чата нет - создаем новый
			var otherUser = await context.Users.FindAsync(userId2);
			if (otherUser == null) return null;

			var newChat = new ChatPreview
			{
				UserId = userId1,
				OtherUserId = userId2,
				OtherUserName = otherUser.UserName,
				LastMessage = "Чат начат",
				LastMessageTime = DateTime.Now,
				UnreadCount = 0
			};

			context.ChatPreviews.Add(newChat);
			await context.SaveChangesAsync();

			return newChat;
		}
		public async Task AcceptFriendRequestAsync(int friendshipId)
		{
			using var context = _contextFactory.CreateDbContext();

			var friendship = await context.Friendships.FindAsync(friendshipId);
			if (friendship != null && friendship.Status == FriendshipStatus.Pending)
			{
				friendship.Status = FriendshipStatus.Accepted;
				friendship.UpdatedAt = DateTime.UtcNow;
				await context.SaveChangesAsync();
			}
		}

		public async Task<List<User>> GetFriendsAsync(int userId)
		{
			using var context = _contextFactory.CreateDbContext();

			var friends = await context.Friendships
				.Where(f => (f.RequesterId == userId || f.AddresseeId == userId) &&
						   f.Status == FriendshipStatus.Accepted)
				.Select(f => f.RequesterId == userId ? f.Addressee : f.Requester)
				.ToListAsync();

			return friends;
		}

		public async Task<Friendship> GetFriendshipStatusAsync(int user1Id, int user2Id)
		{
			using var context = _contextFactory.CreateDbContext();

			return await context.Friendships
				.FirstOrDefaultAsync(f =>
					(f.RequesterId == user1Id && f.AddresseeId == user2Id) ||
					(f.RequesterId == user2Id && f.AddresseeId == user1Id));
		}

		// Реализуйте остальные методы по аналогии...
		public Task RejectFriendRequestAsync(int friendshipId) => UpdateFriendshipStatus(friendshipId, FriendshipStatus.Rejected);
		public Task RemoveFriendAsync(int friendshipId) => UpdateFriendshipStatus(friendshipId, FriendshipStatus.Rejected);
		public Task BlockUserAsync(int currentUserId, int targetUserId) => UpdateFriendshipStatus(currentUserId, targetUserId, FriendshipStatus.Blocked);

		private async Task UpdateFriendshipStatus(int friendshipId, FriendshipStatus status)
		{
			using var context = _contextFactory.CreateDbContext();

			var friendship = await context.Friendships.FindAsync(friendshipId);
			if (friendship != null)
			{
				friendship.Status = status;
				friendship.UpdatedAt = DateTime.UtcNow;
				await context.SaveChangesAsync();
			}
		}

		private async Task UpdateFriendshipStatus(int user1Id, int user2Id, FriendshipStatus status)
		{
			using var context = _contextFactory.CreateDbContext();

			var friendship = await GetFriendshipStatusAsync(user1Id, user2Id);
			if (friendship != null)
			{
				friendship.Status = status;
				friendship.UpdatedAt = DateTime.UtcNow;
				await context.SaveChangesAsync();
			}
		}

		public async Task<List<Friendship>> GetFriendRequestsAsync(int userId)
		{
			using var context = _contextFactory.CreateDbContext();

			return await context.Friendships
				.Include(f => f.Requester)
				.Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
				.ToListAsync();
		}

		public async Task<List<User>> GetOnlineFriendsAsync(int userId)
		{
			using var context = _contextFactory.CreateDbContext();

			var friends = await GetFriendsAsync(userId);
			return friends.Where(f => f.IsOnline).ToList();
		}

		public async Task UnblockUserAsync(int friendshipId)
		{
			using var context = _contextFactory.CreateDbContext();

			var friendship = await context.Friendships.FindAsync(friendshipId);
			if (friendship != null && friendship.Status == FriendshipStatus.Blocked)
			{
				context.Friendships.Remove(friendship);
				await context.SaveChangesAsync();
			}
		}
		public async Task CancelFriendRequestAsync(int friendshipId)
		{
			using var context = _contextFactory.CreateDbContext();

			var friendship = await context.Friendships.FindAsync(friendshipId);
			if (friendship != null && friendship.Status == FriendshipStatus.Pending)
			{
				context.Friendships.Remove(friendship);
				await context.SaveChangesAsync();
			}
		}
		
	}
}

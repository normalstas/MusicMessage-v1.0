using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MusicMessage.Models;
namespace MusicMessage.Repository
{
	public interface IProfileRepository
	{
		Task<User> GetUserProfileAsync(int userId);
		Task UpdateUserProfileAsync(User user);
		Task<List<User>> GetMutualFriendsAsync(int userId1, int userId2);
		Task<int> GetFriendsCountAsync(int userId);
		Task<int> GetPostsCountAsync(int userId);
		Task<string> UploadAvatarAsync(int userId, byte[] imageData);
		Task<string> UploadCoverAsync(int userId, byte[] imageData);
	}
	public class ProfileRepository : IProfileRepository
	{
		private readonly IDbContextFactory<MessangerBaseContext> _contextFactory;

		public ProfileRepository(IDbContextFactory<MessangerBaseContext> contextFactory)
		{
			_contextFactory = contextFactory;
		}

		public async Task<User> GetUserProfileAsync(int userId)
		{
			using var context = _contextFactory.CreateDbContext();
			return await context.Users
				.FirstOrDefaultAsync(u => u.UserId == userId);
		}

		public async Task UpdateUserProfileAsync(User user)
		{
			using var context = _contextFactory.CreateDbContext();

			var existingUser = await context.Users.FindAsync(user.UserId);
			if (existingUser != null)
			{
				// ОБРЕЗАЕМ СТРОКИ ДО МАКСИМАЛЬНОЙ ДЛИНЫ
				existingUser.FirstName = TruncateString(user.FirstName, 100);
				existingUser.LastName = TruncateString(user.LastName, 100);
				existingUser.DateOfBirth = user.DateOfBirth;
				existingUser.Gender = TruncateString(user.Gender, 10);
				existingUser.City = TruncateString(user.City, 100);
				existingUser.Country = TruncateString(user.Country, 100);
				existingUser.Bio = TruncateString(user.Bio, 500);
				existingUser.AvatarPath = user.AvatarPath;
				existingUser.ProfileCoverPath = user.ProfileCoverPath; // ДОБАВЬТЕ ЭТУ СТРОЧКУ

				await context.SaveChangesAsync();

			}
		}
		private string TruncateString(string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value)) return value;
			return value.Length <= maxLength ? value : value.Substring(0, maxLength);
		}

		public async Task<List<User>> GetMutualFriendsAsync(int userId1, int userId2)
		{
			using var context = _contextFactory.CreateDbContext();

			var user1Friends = await context.Friendships
				.Where(f => (f.RequesterId == userId1 || f.AddresseeId == userId1) &&
						   f.Status == FriendshipStatus.Accepted)
				.Select(f => f.RequesterId == userId1 ? f.AddresseeId : f.RequesterId)
				.ToListAsync();

			var user2Friends = await context.Friendships
				.Where(f => (f.RequesterId == userId2 || f.AddresseeId == userId2) &&
						   f.Status == FriendshipStatus.Accepted)
				.Select(f => f.RequesterId == userId2 ? f.AddresseeId : f.RequesterId)
				.ToListAsync();

			var mutualFriendIds = user1Friends.Intersect(user2Friends).ToList();

			return await context.Users
				.Where(u => mutualFriendIds.Contains(u.UserId))
				.ToListAsync();
		}

		public async Task<int> GetFriendsCountAsync(int userId)
		{
			using var context = _contextFactory.CreateDbContext();
			return await context.Friendships
				.CountAsync(f => (f.RequesterId == userId || f.AddresseeId == userId) &&
							   f.Status == FriendshipStatus.Accepted);
		}

		public async Task<int> GetPostsCountAsync(int userId)
		{
			using var context = _contextFactory.CreateDbContext();
			return await context.Messages
				.CountAsync(m => m.SenderId == userId &&
							   m.MessageType == "Text" &&
							   !m.IsDeletedForEveryone);
		}

		public async Task<string> UploadAvatarAsync(int userId, byte[] imageData)
		{
			// Реализация загрузки аватара
			var fileName = $"avatar_{userId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
			var filePath = Path.Combine("Avatars", fileName);

			Directory.CreateDirectory("Avatars");
			await File.WriteAllBytesAsync(filePath, imageData);

			return filePath;
		}

		public async Task<string> UploadCoverAsync(int userId, byte[] imageData)
		{
			// Реализация загрузки обложки
			var fileName = $"cover_{userId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
			var filePath = Path.Combine("Covers", fileName);

			Directory.CreateDirectory("Covers");
			await File.WriteAllBytesAsync(filePath, imageData);

			return filePath;
		}
	}
}

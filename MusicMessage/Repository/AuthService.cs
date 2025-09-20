using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MusicMessage.Models;
namespace MusicMessage.Repository
{
	public interface IAuthService
	{
		Task<User> LoginAsync(string username, string password);
		Task<User> RegisterAsync(string username, string email, string password, string firstname, string lastname);
		void Logout();
		User CurrentUser { get; }
		bool IsLoggedIn { get; }
	}
	public class AuthService : IAuthService
	{
		private readonly IDbContextFactory<MessangerBaseContext> _contextFactory;
		private User _currentUser;

		public AuthService(IDbContextFactory<MessangerBaseContext> contextFactory)
		{
			_contextFactory = contextFactory;
		}

		public User CurrentUser => _currentUser;
		public bool IsLoggedIn => _currentUser != null;

		public async Task<User> LoginAsync(string username, string password)
		{
			using var context = _contextFactory.CreateDbContext();
			// Используем ConfigureAwait(false) для операций с БД
			var user = await context.Users
				.FirstOrDefaultAsync(u => u.UserName == username)
				.ConfigureAwait(false);

			if (user == null)
				return null;

			var passwordHash = HashPassword(password);
			if (user.PasswordHash != passwordHash)
				return null;

			_currentUser = user;
			user.LastLogin = DateTime.UtcNow;

			// Сохраняем изменения асинхронно
			await context.SaveChangesAsync().ConfigureAwait(false);

			return user;
		}

		public async Task<User> RegisterAsync(string username, string email, string password, string firstname,string lastname)
		{
			using var context = _contextFactory.CreateDbContext();
			// Проверка на существующего пользователя
			if (await context.Users.AnyAsync(u => u.UserName == username || u.Email == email))
				return null;

			var newUser = new User
			{
				UserName = username,
				Email = email,
				PasswordHash = HashPassword(password),
				FirstName = firstname,
				LastName = lastname,
				CreatedAt = DateTime.UtcNow
			};

			context.Users.Add(newUser);
			await context.SaveChangesAsync();

			// Не логинимся автоматически после регистрации?
			// _currentUser = newUser;
			return newUser;
		}

		public void Logout()
		{
			_currentUser = null;
		}

		private string HashPassword(string password)
		{
			// ВРЕМЕННАЯ РЕАЛИЗАЦИЯ. ДЛЯ ПРОДУКЦИИ ИСПОЛЬЗУЙТЕ BCrypt или PBKDF2!
			using (var sha256 = SHA256.Create())
			{
				var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
				return Convert.ToBase64String(bytes);
			}
		}
	}
}

using Microsoft.EntityFrameworkCore;
using MusicMessage.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MusicMessage.Repository
{
	public interface IChatRepository
	{
		Task<List<ChatPreview>> GetUserChatsAsync(int userId);
		Task<ChatPreview> GetOrCreateChatAsync(int userId1, int userId2);
		Task UpdateChatUnreadCountAsync(int userId, int otherUserId, int unreadCount);
		Task<int> RecalculateUnreadCountAsync(int userId, int otherUserId);
		Task CreateChatPreviewAsync(int userId, int otherUserId);
		Task UpdateAllChatsLastMessagesAsync(int userId);

	}
	public class ChatRepository : IChatRepository
	{
		private readonly IDbContextFactory<MessangerBaseContext> _contextFactory;
		private readonly object _lockObject = new object();
		public ChatRepository(IDbContextFactory<MessangerBaseContext> contextFactory)
		{
			_contextFactory = contextFactory;
		}
		public async Task CreateChatPreviewAsync(int userId, int otherUserId)
		{
			using var context = _contextFactory.CreateDbContext();
			var otherUser = await context.Users.FindAsync(otherUserId).ConfigureAwait(false);
			if (otherUser == null) return;

			var existingChat = await context.ChatPreviews
				.FirstOrDefaultAsync(c => c.UserId == userId && c.OtherUserId == otherUserId)
				.ConfigureAwait(false);

			if (existingChat == null)
			{
				var newChat = new ChatPreview
				{
					UserId = userId,
					OtherUserId = otherUserId,
					OtherUserName = otherUser.UserName,
					LastMessage = "Чат начат",
					LastMessageTime = DateTime.Now,
					UnreadCount = 0
				};

				context.ChatPreviews.Add(newChat);
				await context.SaveChangesAsync();
			}
		}

		public async Task UpdateAllChatsLastMessagesAsync(int userId)
		{
			using var context = _contextFactory.CreateDbContext();
			var userChats = await context.ChatPreviews
				.Where(c => c.UserId == userId)
				.ToListAsync()
				;

			foreach (var chat in userChats)
			{
				try
				{
					var lastMessage = await context.Messages
						.Include(m => m.Sender)
						.Where(m => (m.SenderId == userId && m.ReceiverId == chat.OtherUserId) ||
								   (m.SenderId == chat.OtherUserId && m.ReceiverId == userId))
						.Where(m => !m.IsDeletedForEveryone &&
								   !(m.IsDeletedForSender && m.SenderId == userId) &&
								   !(m.IsDeletedForReceiver && m.ReceiverId == userId))
						.OrderByDescending(m => m.Timestamp)
						.FirstOrDefaultAsync()
						;

					if (lastMessage != null)
					{
						string senderPrefix = lastMessage.SenderId == userId
							? "Вы: "
							: $"{lastMessage.Sender?.UserName ?? "Unknown"}: ";

						if (lastMessage.IsVoiceMessage)
						{
							chat.LastMessage = senderPrefix + "🎤 Голосовое сообщение";
						}
						else if (!string.IsNullOrEmpty(lastMessage.StickerId))
						{
							chat.LastMessage = senderPrefix + "🖼️ Стикер";
						}
						else
						{
							var messageContent = lastMessage.ContentMess?.Length > 30
								? lastMessage.ContentMess.Substring(0, 30) + "..."
								: lastMessage.ContentMess;

							chat.LastMessage = senderPrefix + messageContent;
						}

						chat.LastMessageTime = lastMessage.Timestamp;
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Ошибка обработки чата {chat.OtherUserId}: {ex.Message}");
					// Продолжаем обработку остальных чатов
					continue;
				}
			}

			await context.SaveChangesAsync();

		}
		public async Task<List<ChatPreview>> GetUserChatsAsync(int userId)
		{
			using var context = _contextFactory.CreateDbContext();
			// Получаем все чаты пользователя
			var userChats = await context.ChatPreviews
			.Include(c => c.OtherUser)
			.Where(c => c.UserId == userId)
			.AsNoTracking() // ДОБАВЬТЕ ЭТО
			.ToListAsync()
			.ConfigureAwait(false);

			// Для каждого чата находим последнее реальное сообщение из базы
			foreach (var chat in userChats)
			{
				var lastMessage = await context.Messages
					.Include(m => m.Sender)
					.Where(m => (m.SenderId == userId && m.ReceiverId == chat.OtherUserId) ||
							   (m.SenderId == chat.OtherUserId && m.ReceiverId == userId))
					.Where(m => !m.IsDeletedForEveryone &&
							   !(m.IsDeletedForSender && m.SenderId == userId) &&
							   !(m.IsDeletedForReceiver && m.ReceiverId == userId))
					.OrderByDescending(m => m.Timestamp)
					.AsNoTracking() // ДОБАВЬТЕ И ЗДЕСЬ
					.FirstOrDefaultAsync()
					.ConfigureAwait(false);

				if (lastMessage != null)
				{
					string senderPrefix = lastMessage.SenderId == userId
						? "Вы: "
						: $"{lastMessage.Sender?.UserName ?? "Unknown"}: ";

					if (lastMessage.IsVoiceMessage)
					{
						chat.LastMessage = senderPrefix + "🎤 Голосовое сообщение";
					}
					else if (!string.IsNullOrEmpty(lastMessage.StickerId))
					{
						chat.LastMessage = senderPrefix + "🖼️ Стикер";
					}
					else
					{
						var messageContent = lastMessage.ContentMess?.Length > 30
							? lastMessage.ContentMess.Substring(0, 30) + "..."
							: lastMessage.ContentMess;

						chat.LastMessage = senderPrefix + messageContent;
					}

					chat.LastMessageTime = lastMessage.Timestamp;
				}
			}

			return userChats.OrderByDescending(c => c.LastMessageTime).ToList();


		}

		public async Task<int> RecalculateUnreadCountAsync(int userId, int otherUserId)
		{
			using var context = _contextFactory.CreateDbContext();

			try
			{
				// ПРОСТОЙ подсчет - только непрочитанные сообщения ОТ собеседника
				var unreadCount = await context.Messages
					.CountAsync(m => m.SenderId == otherUserId &&   // ОТ собеседника
								   m.ReceiverId == userId &&        // МНЕ
								   !m.IsRead &&                    // НЕ прочитано
								   !m.IsDeletedForEveryone)
					.ConfigureAwait(false);

				

				return unreadCount;
			}
			catch (Exception ex)
			{
				
				return 0;
			}
		}

		public async Task<ChatPreview> GetOrCreateChatAsync(int userId1, int userId2)
		{
			using var context = _contextFactory.CreateDbContext();
			var chat = await context.Messages
				.Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
						   (m.SenderId == userId2 && m.ReceiverId == userId1))
				.OrderByDescending(m => m.Timestamp)
				.Select(m => new ChatPreview
				{
					OtherUserId = m.SenderId == userId1 ? m.ReceiverId : m.SenderId,
					OtherUserName = m.SenderId == userId1 ? m.Receiver.UserName : m.Sender.UserName,
					LastMessage = m.ContentMess,
					LastMessageTime = m.Timestamp
				})
				.FirstOrDefaultAsync()
				.ConfigureAwait(false);

			if (chat == null)
			{
				// Создаем новый чат
				var otherUser = await context.Users.FindAsync(userId2);
				chat = new ChatPreview
				{
					OtherUserId = userId2,
					OtherUserName = otherUser.UserName,
					LastMessage = "Чат начат",
					LastMessageTime = DateTime.Now
				};
			}

			return chat;
		}
		public async Task UpdateChatUnreadCountAsync(int userId, int otherUserId, int unreadCount)
		{
			using var context = _contextFactory.CreateDbContext();
			// НАЙДИТЕ И ОБНОВИТЕ ChatPreview в базе данных
			var chatPreview = await context.ChatPreviews
				.FirstOrDefaultAsync(c => c.UserId == userId && c.OtherUserId == otherUserId);

			if (chatPreview != null)
			{
				chatPreview.UnreadCount = unreadCount;
				await context.SaveChangesAsync();
			}
			else
			{
				// Если чата нет, создаем его
				var otherUser = await context.Users.FindAsync(otherUserId);
				if (otherUser != null)
				{
					var newChat = new ChatPreview
					{
						UserId = userId,
						OtherUserId = otherUserId,
						OtherUserName = otherUser.UserName,
						LastMessage = "Чат начат",
						LastMessageTime = DateTime.Now,
						UnreadCount = unreadCount
					};
					context.ChatPreviews.Add(newChat);
					await context.SaveChangesAsync();
				}
			}
		}
	}
}

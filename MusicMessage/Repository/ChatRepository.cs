using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
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

			// Проверяем и создаем чат для ОТПРАВИТЕЛЯ (userId)
			var existingChatForSender = await context.ChatPreviews
				.FirstOrDefaultAsync(c => c.UserId == userId && c.OtherUserId == otherUserId)
				.ConfigureAwait(false);

			if (existingChatForSender == null)
			{
				var newChatForSender = new ChatPreview
				{
					UserId = userId,
					OtherUserId = otherUserId,
					OtherUserName = otherUser.UserName,
					AvatarPath = otherUser.AvatarPath,
					FirstName = otherUser.FirstName,
					LastName = otherUser.LastName,
					LastMessage = "Чат начат",
					LastMessageTime = DateTime.Now,
					UnreadCount = 0
				};
				context.ChatPreviews.Add(newChatForSender);
			}

			// Проверяем и создаем чат для ПОЛУЧАТЕЛЯ (otherUserId)
			var existingChatForReceiver = await context.ChatPreviews
				.FirstOrDefaultAsync(c => c.UserId == otherUserId && c.OtherUserId == userId)
				.ConfigureAwait(false);

			if (existingChatForReceiver == null)
			{
				var currentUser = await context.Users.FindAsync(userId).ConfigureAwait(false);
				if (currentUser != null)
				{
					var newChatForReceiver = new ChatPreview
					{
						UserId = otherUserId,
						OtherUserId = userId,
						OtherUserName = currentUser.UserName,
						AvatarPath = currentUser.AvatarPath,
						FirstName = currentUser.FirstName,
						LastName = currentUser.LastName,
						LastMessage = "Чат начат",
						LastMessageTime = DateTime.Now,
						UnreadCount = 0
					};
					context.ChatPreviews.Add(newChatForReceiver);
				}
			}

			await context.SaveChangesAsync();
		}

		public async Task UpdateAllChatsLastMessagesAsync(int userId)
		{
			using var context = _contextFactory.CreateDbContext();

			// Получаем ID всех чатов пользователя
			var chatIds = await context.ChatPreviews
				.Where(c => c.UserId == userId)
				.Select(c => c.ChatPreviewId)
				.ToListAsync();

			foreach (var chatId in chatIds)
			{
				try
				{
					// Находим чат в текущем контексте для обновления
					var chat = await context.ChatPreviews.FindAsync(chatId);
					if (chat == null) continue;

					var lastMessage = await context.Messages
						.Include(m => m.Sender)
						.Where(m => (m.SenderId == userId && m.ReceiverId == chat.OtherUserId) ||
								   (m.SenderId == chat.OtherUserId && m.ReceiverId == userId))
						.Where(m => !m.IsDeletedForEveryone &&
								   !(m.IsDeletedForSender && m.SenderId == userId) &&
								   !(m.IsDeletedForReceiver && m.ReceiverId == userId))
						.OrderByDescending(m => m.Timestamp)
						.AsNoTracking()
						.FirstOrDefaultAsync();

					if (lastMessage != null)
					{
						string senderPrefix = lastMessage.SenderId == userId
							? "Вы: "
							: $"{lastMessage.Sender?.FirstName ?? "Unknown"}: ";

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
					else
					{
						// Если сообщений нет, но чат существует - устанавливаем значения по умолчанию
						chat.LastMessage = "Чат начат";
						chat.LastMessageTime = DateTime.Now;
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Ошибка обработки чата {chatId}: {ex.Message}");
					continue;
				}
			}

			try
			{
				await context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException ex)
			{
				// Обрабатываем ошибки параллелизма
				foreach (var entry in ex.Entries)
				{
					entry.State = EntityState.Detached;
				}
				Debug.WriteLine("Конфликт параллелизма при обновлении чатов");
			}
			catch (DbUpdateException ex)
			{
				Debug.WriteLine($"Ошибка сохранения чатов: {ex.Message}");
			}
		}

		public async Task<List<ChatPreview>> GetUserChatsAsync(int userId)
		{
			using var context = _contextFactory.CreateDbContext();

			// Получаем все чаты пользователя только для чтения
			var userChats = await context.ChatPreviews
				.Include(c => c.OtherUser)
				.Where(c => c.UserId == userId)
				.AsNoTracking()
				.ToListAsync();

			var validChats = new List<ChatPreview>();
			var chatsToDeleteIds = new List<int>();

			foreach (var chat in userChats)
			{
				// Заполняем недостающие данные из OtherUser
				if (string.IsNullOrEmpty(chat.FirstName) || string.IsNullOrEmpty(chat.LastName))
				{
					chat.FirstName = chat.OtherUser?.FirstName;
					chat.LastName = chat.OtherUser?.LastName;
				}

				// Проверяем, есть ли НЕУДАЛЕННЫЕ сообщения в чате
				var hasNonDeletedMessages = await context.Messages
					.AsNoTracking()
					.AnyAsync(m => ((m.SenderId == userId && m.ReceiverId == chat.OtherUserId) ||
								  (m.SenderId == chat.OtherUserId && m.ReceiverId == userId)) &&
								 !m.IsDeletedForEveryone &&
								 !(m.IsDeletedForSender && m.SenderId == userId) &&
								 !(m.IsDeletedForReceiver && m.ReceiverId == userId));

				if (hasNonDeletedMessages)
				{
					validChats.Add(chat);
				}
				else
				{
					chatsToDeleteIds.Add(chat.ChatPreviewId);
				}
			}

			// Удаляем чаты без сообщений в ОТДЕЛЬНОЙ операции
			if (chatsToDeleteIds.Any())
			{
				await DeleteChatsWithoutMessages(chatsToDeleteIds);
			}

			return validChats.OrderByDescending(c => c.LastMessageTime).ToList();
		}
		private async Task DeleteChatsWithoutMessages(List<int> chatIdsToDelete)
		{
			using var deleteContext = _contextFactory.CreateDbContext();

			// Находим чаты для удаления в этом контексте
			var chatsToDelete = await deleteContext.ChatPreviews
				.Where(c => chatIdsToDelete.Contains(c.ChatPreviewId))
				.ToListAsync();

			if (chatsToDelete.Any())
			{
				deleteContext.ChatPreviews.RemoveRange(chatsToDelete);

				try
				{
					await deleteContext.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException ex)
				{
					// Игнорируем ошибки параллелизма - чаты уже могли быть удалены
					foreach (var entry in ex.Entries)
					{
						entry.State = EntityState.Detached;
					}
				}
				catch (DbUpdateException ex)
				{
					// Логируем ошибку, но не прерываем выполнение
					Debug.WriteLine($"Ошибка удаления чатов: {ex.Message}");
				}
			}
		}
		private async Task UpdateChatLastMessageAsync(MessangerBaseContext context, ChatPreview chat, int userId)
		{
			var lastMessage = await context.Messages
				.Include(m => m.Sender)
				.Where(m => ((m.SenderId == userId && m.ReceiverId == chat.OtherUserId) ||
						   (m.SenderId == chat.OtherUserId && m.ReceiverId == userId)) &&
						  !m.IsDeletedForEveryone &&
						  !(m.IsDeletedForSender && m.SenderId == userId) &&
						  !(m.IsDeletedForReceiver && m.ReceiverId == userId))
				.OrderByDescending(m => m.Timestamp)
				.FirstOrDefaultAsync();

			if (lastMessage != null)
			{
				string senderPrefix = lastMessage.SenderId == userId
					? "Вы: "
					: $"{lastMessage.Sender?.FirstName ?? "Unknown"}: ";

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
			else
			{
				// Если сообщений нет, но чат остался (например, только что созданный)
				chat.LastMessage = "Чат начат";
				chat.LastMessageTime = DateTime.Now;
			}

			// Сохраняем изменения
			context.ChatPreviews.Update(chat);
		}

		public async Task<int> RecalculateUnreadCountAsync(int userId, int otherUserId)
		{
			using var context = _contextFactory.CreateDbContext();

			try
			{
				// ПРАВИЛЬНЫЙ подсчет - только непрочитанные сообщения ОТ собеседника
				// которые НЕ удалены для получателя
				var unreadCount = await context.Messages
					.CountAsync(m => m.SenderId == otherUserId &&   // ОТ собеседника
								   m.ReceiverId == userId &&        // МНЕ
								   !m.IsRead &&                    // НЕ прочитано
								   !m.IsDeletedForEveryone &&      // НЕ удалено для всех
								   !(m.IsDeletedForReceiver && m.ReceiverId == userId)) // НЕ удалено для меня
					.ConfigureAwait(false);

				return unreadCount;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"RecalculateUnreadCountAsync error: {ex.Message}");
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

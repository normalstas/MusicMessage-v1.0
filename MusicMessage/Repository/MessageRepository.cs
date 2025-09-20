using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MusicMessage.Models;
using System.IO;
namespace MusicMessage.Repository
{
	public interface IMessageRepository
	{
		Task<IEnumerable<Message>> GetAllMessagesAsync(int senderId, int receiverId);
		Task AddTextMessageAsync(string text, int senderId, int receiverId);
		Task AddVoiceMessageAsync(string audioPath, TimeSpan duration, int senderId, int receiverId);
		Task AddStickerMessageAsync(string sticer, int senderId, int receiverId);
		//Task AddReactionAsync(int messageId, string emoji);
		Task UpdateMessageAsync(Message message);
		Task SaveWaveformDataAsync(int messageId, List<double> waveform);
		Task<List<double>> GetWaveformDataAsync(int messageId);
		Task<Message> GetMessageByIdAsync(int messageId);
		Task AddReplyMessageAsync(string text, int senderId, int receiverId, int? replyToMessageId);
		Task DeleteMessageForMeAsync(int messageId, int userId);
		Task DeleteMessageForEveryoneAsync(int messageId);
		Task DeleteMessageForReceiverAsync(int messageId, int userId);
		Task UpdateMessageTextAsync(int messageId, string newText);
		Task AddOrUpdateReactionAsync(int messageId, int userId, string emoji);
		Task RemoveReactionAsync(int messageId, int userId);
		Task<Message> GetLastMessageForUserAsync(int userId);
		Task<int> AddReplyMessageAndGetIdAsync(string text, int senderId, int receiverId, int? replyToMessageId);
		Task<Dictionary<int, List<Reaction>>> GetReactionsForMessagesAsync(List<int> messageIds);
		Task UpdateMessageReadStatusAsync(int messageId, bool isRead);
		Task DeleteAllMessagesForEveryoneAsync(int userId, int otherUserId);
	}

	public class MessageRepository : IMessageRepository
	{
		private readonly IDbContextFactory<MessangerBaseContext> _contextFactory;

		public MessageRepository(IDbContextFactory<MessangerBaseContext> contextFactory)
		{
			_contextFactory = contextFactory;
		}

		public async Task AddTextMessageAsync(string text, int senderId, int receiverId)
		{
			using var context = _contextFactory.CreateDbContext();
			var message = new Message
			{
				ContentMess = text,
				SenderId = senderId,
				ReceiverId = receiverId, // Добавляем получателя
				MessageType = "Text",
				Timestamp = DateTime.Now
			};
			await context.Messages.AddAsync(message);
			await context.SaveChangesAsync();
		}
		public async Task AddVoiceMessageAsync(string audioPath, TimeSpan duration, int senderId, int receiverId)
		{
			using var context = _contextFactory.CreateDbContext();
			var message = new Message
			{
				MessageType = "Voice", // Явно указываем тип
				AudioPath = audioPath,
				Duration = duration,
				SenderId = senderId,
				ReceiverId = receiverId,
				Timestamp = DateTime.Now,
				ContentMess = "" // Очищаем текстовое содержимое
			};	
			await context.Messages.AddAsync(message);
			await context.SaveChangesAsync();
		}

		public async Task SaveWaveformDataAsync(int messageId, List<double> waveform)
		{
			using var context = _contextFactory.CreateDbContext();
			var message = await context.Messages.FindAsync(messageId);
			if (message != null)
			{
				message.WaveformData = waveform != null && waveform.Count > 0
					? string.Join(",", waveform.Select(d => d.ToString(CultureInfo.InvariantCulture)))
					: null;
				await context.SaveChangesAsync();
			}
		}
		public async Task UpdateMessageTextAsync(int messageId, string newText)
		{
			using var context = _contextFactory.CreateDbContext();
			var message = await context.Messages.FindAsync(messageId);
			if (message != null)
			{
				message.ContentMess = newText;
				// Не обновляем Timestamp!
				await context.SaveChangesAsync();
			}
		}
		public async Task<List<double>> GetWaveformDataAsync(int messageId)
		{
			using var context = _contextFactory.CreateDbContext();
			var message = await context.Messages.FindAsync(messageId);
			if (message == null || string.IsNullOrEmpty(message.WaveformData))
				return GenerateRandomWaveform();

			return message.WaveformData.Split(',', StringSplitOptions.RemoveEmptyEntries)
									 .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
									 .ToList();
		}
		private List<double> GenerateRandomWaveform()
		{
			using var context = _contextFactory.CreateDbContext();
			var random = new Random();
			return Enumerable.Range(0, 30)
						   .Select(_ => (double)random.Next(5, 25))
						   .ToList();
		}
		public async Task<Message> GetMessageByIdAsync(int messageId)
		{
			using var context = _contextFactory.CreateDbContext();
			return await context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.ReplyToMessage)  // Это критически важно!
					.ThenInclude(rm => rm.Sender) // И это тоже!
				.FirstOrDefaultAsync(m => m.MessageId == messageId);
		}
		public async Task UpdateMessageReadStatusAsync(int messageId, bool isRead)
		{
			using var context = _contextFactory.CreateDbContext();
			var message = await context.Messages.FindAsync(messageId);
			if (message != null)
			{
				message.IsRead = isRead;
				await context.SaveChangesAsync();
			}
		}
		public async Task AddReplyMessageAsync(string text, int senderId, int receiverId, int? replyToMessageId)
		{
			using var context = _contextFactory.CreateDbContext();
			var message = new Message
			{
				ContentMess = text,
				SenderId = senderId,
				ReceiverId = receiverId,
				MessageType = "Text",
				Timestamp = DateTime.Now,
				ReplyToMessageId = replyToMessageId
			};

			await context.Messages.AddAsync(message);
			await context.SaveChangesAsync();
		}
		public async Task DeleteMessageForMeAsync(int messageId, int userId)
		{
			using var context = _contextFactory.CreateDbContext();
			var message = await context.Messages.FindAsync(messageId);
			if (message != null && message.SenderId == userId)
			{
				message.IsDeletedForSender = true;
				await context.SaveChangesAsync();
			}
		}

		public async Task DeleteMessageForEveryoneAsync(int messageId)
		{
			using var context = _contextFactory.CreateDbContext();
			var message = await context.Messages.FindAsync(messageId);
			if (message != null)
			{
				message.IsDeletedForEveryone = true;
				await context.SaveChangesAsync();
			}
		}

		public async Task DeleteMessageForReceiverAsync(int messageId, int userId)
		{
			using var context = _contextFactory.CreateDbContext();
			var message = await context.Messages.FindAsync(messageId);
			if (message != null && message.ReceiverId == userId)
			{
				message.IsDeletedForReceiver = true;
				await context.SaveChangesAsync();
			}
		}
		public async Task DeleteAllMessagesForEveryoneAsync(int userId, int otherUserId)
		{
			using var context = _contextFactory.CreateDbContext();

			var messagesToDelete = await context.Messages
				.Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
						   (m.SenderId == otherUserId && m.ReceiverId == userId))
				.ToListAsync();

			foreach (var message in messagesToDelete)
			{
				message.IsDeletedForEveryone = true;

				// Для голосовых сообщений удаляем файлы
				if (message.IsVoiceMessage && !string.IsNullOrEmpty(message.AudioPath))
				{
					try
					{
						var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, message.AudioPath);
						if (File.Exists(fullPath))
						{
							File.Delete(fullPath);
						}
					}
					catch
					{
						// Игнорируем ошибки удаления файла
					}
				}
			}

			await context.SaveChangesAsync();
		}
		public async Task AddOrUpdateReactionAsync(int messageId, int userId, string emoji)
		{
			using var context = _contextFactory.CreateDbContext();
			// Находим существующую реакцию пользователя на это сообщение
			var existingReaction = await context.Reactions
				.FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

			if (existingReaction != null)
			{
				// Если реакция уже есть - меняем эмодзи
				existingReaction.Emoji = emoji;
				context.Reactions.Update(existingReaction);
			}
			else
			{
				// Если реакции нет - создаем новую
				var newReaction = new Reaction
				{
					MessageId = messageId,
					UserId = userId,
					Emoji = emoji
				};
				await context.Reactions.AddAsync(newReaction);
			}

			await context.SaveChangesAsync();
		}

		public async Task RemoveReactionAsync(int messageId, int userId)
		{
			using var context = _contextFactory.CreateDbContext();
			var reactionToRemove = await context.Reactions
				.FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

			if (reactionToRemove != null)
			{
				context.Reactions.Remove(reactionToRemove);
				await context.SaveChangesAsync();
			}
		}

		public async Task<Dictionary<int, List<Reaction>>> GetReactionsForMessagesAsync(List<int> messageIds)
		{
			using var context = _contextFactory.CreateDbContext();
			if (messageIds == null || !messageIds.Any())
				return new Dictionary<int, List<Reaction>>();

			// Убедитесь, что загружаются актуальные данные из базы
			var reactions = await context.Reactions
	   .Include(r => r.User) // КРИТИЧЕСКИ ВАЖНО
	   .Where(r => messageIds.Contains(r.MessageId))
	   .ToListAsync();

			return reactions
				.GroupBy(r => r.MessageId)
				.ToDictionary(g => g.Key, g => g.ToList());
		}
		public async Task AddStickerMessageAsync(string sticer, int senderId, int receiverId)
		{
			using var context = _contextFactory.CreateDbContext();
			var message = new Message
			{
				StickerId = sticer,
				SenderId = senderId,
				ReceiverId = receiverId,
				MessageType = "Sticer",
			};
			await context.Messages.AddAsync(message);
			await context.SaveChangesAsync();
		}
		
		public async Task UpdateMessageAsync(Message message)
		{
			using var context = _contextFactory.CreateDbContext();
			context.Messages.Update(message);
			await context.SaveChangesAsync();
		}
		public async Task<Message> GetLastMessageForUserAsync(int userId)
		{
			using var context = _contextFactory.CreateDbContext();
			return await context.Messages
				.Where(m => m.SenderId == userId)
				.OrderByDescending(m => m.Timestamp)
				.FirstOrDefaultAsync();
		}
		public async Task<int> AddReplyMessageAndGetIdAsync(string text, int senderId, int receiverId, int? replyToMessageId)
		{
			using var context = _contextFactory.CreateDbContext();
			var message = new Message
			{
				ContentMess = text,
				SenderId = senderId,
				ReceiverId = receiverId,
				MessageType = "Text",
				Timestamp = DateTime.Now,
				ReplyToMessageId = replyToMessageId
			};

			await context.Messages.AddAsync(message);
			await context.SaveChangesAsync();

			// Возвращаем ID сохраненного сообщения
			return message.MessageId;
		}
		public async Task<IEnumerable<Message>> GetAllMessagesAsync(int senderId, int receiverId)
		{
			using var context = _contextFactory.CreateDbContext();
			try
			{
				if (senderId == 0 || receiverId == 0)
					return new List<Message>();

				

				var messages = await context.Messages
					.Include(m => m.Sender)
					.Include(m => m.Receiver)
					.Include(m => m.MessageReactions)
						.ThenInclude(r => r.User)
					.Include(m => m.ReplyToMessage)
						.ThenInclude(rm => rm.Sender)
					.Where(m => (m.SenderId == senderId && m.ReceiverId == receiverId) ||
							   (m.SenderId == receiverId && m.ReceiverId == senderId))
					.OrderByDescending(m => m.Timestamp)
					.Take(100)
					.OrderBy(m => m.Timestamp)
					.AsNoTracking() // ДОБАВЬТЕ ЭТО
					.ToListAsync();

				return messages ?? new List<Message>();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"GetAllMessagesAsync error: {ex.Message}");
				return new List<Message>();
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MusicMessage.Models;
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
	}

	public class MessageRepository : IMessageRepository
	{
		private readonly MessangerBaseContext _db;

		public MessageRepository(MessangerBaseContext db)
		{
			_db = db;
		}

		public async Task AddTextMessageAsync(string text, int senderId, int receiverId)
		{
			var message = new Message
			{
				ContentMess = text,
				SenderId = senderId,
				ReceiverId = receiverId, // Добавляем получателя
				MessageType = "Text",
				Timestamp = DateTime.Now
			};
			await _db.Messages.AddAsync(message);
			await _db.SaveChangesAsync();
		}
		public async Task AddVoiceMessageAsync(string audioPath, TimeSpan duration, int senderId, int receiverId)
		{
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
			await _db.Messages.AddAsync(message);
			await _db.SaveChangesAsync();
		}
		public async Task SaveWaveformDataAsync(int messageId, List<double> waveform)
		{
			var message = await _db.Messages.FindAsync(messageId);
			if (message != null)
			{
				message.WaveformData = waveform != null && waveform.Count > 0
					? string.Join(",", waveform.Select(d => d.ToString(CultureInfo.InvariantCulture)))
					: null;
				await _db.SaveChangesAsync();
			}
		}
		public async Task UpdateMessageTextAsync(int messageId, string newText)
		{
			var message = await _db.Messages.FindAsync(messageId);
			if (message != null)
			{
				message.ContentMess = newText;
				// Не обновляем Timestamp!
				await _db.SaveChangesAsync();
			}
		}
		public async Task<List<double>> GetWaveformDataAsync(int messageId)
		{
			var message = await _db.Messages.FindAsync(messageId);
			if (message == null || string.IsNullOrEmpty(message.WaveformData))
				return new List<double>();

			return message.WaveformData.Split(',', StringSplitOptions.RemoveEmptyEntries)
									 .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
									 .ToList();
		}

		public async Task<Message> GetMessageByIdAsync(int messageId)
		{
			return await _db.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.ReplyToMessage)  // Добавьте эту строку
					.ThenInclude(rm => rm.Sender) // И эту
				.FirstOrDefaultAsync(m => m.MessageId == messageId);
		}

		public async Task AddReplyMessageAsync(string text, int senderId, int receiverId, int? replyToMessageId)
		{
			var message = new Message
			{
				ContentMess = text,
				SenderId = senderId,
				ReceiverId = receiverId,
				MessageType = "Text",
				Timestamp = DateTime.Now,
				ReplyToMessageId = replyToMessageId
			};

			await _db.Messages.AddAsync(message);
			await _db.SaveChangesAsync();
		}
		public async Task DeleteMessageForMeAsync(int messageId, int userId)
		{
			var message = await _db.Messages.FindAsync(messageId);
			if (message != null && message.SenderId == userId)
			{
				message.IsDeletedForSender = true;
				await _db.SaveChangesAsync();
			}
		}

		public async Task DeleteMessageForEveryoneAsync(int messageId)
		{
			var message = await _db.Messages.FindAsync(messageId);
			if (message != null)
			{
				message.IsDeletedForEveryone = true;
				await _db.SaveChangesAsync();
			}
		}

		public async Task DeleteMessageForReceiverAsync(int messageId, int userId)
		{
			var message = await _db.Messages.FindAsync(messageId);
			if (message != null && message.ReceiverId == userId)
			{
				message.IsDeletedForReceiver = true;
				await _db.SaveChangesAsync();
			}
		}
		public async Task AddOrUpdateReactionAsync(int messageId, int userId, string emoji)
		{
			// Находим существующую реакцию пользователя на это сообщение
			var existingReaction = await _db.Reactions
				.FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

			if (existingReaction != null)
			{
				// Если реакция уже есть - меняем эмодзи
				existingReaction.Emoji = emoji;
				_db.Reactions.Update(existingReaction);
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
				await _db.Reactions.AddAsync(newReaction);
			}

			await _db.SaveChangesAsync();
		}

		public async Task RemoveReactionAsync(int messageId, int userId)
		{
			var reactionToRemove = await _db.Reactions
				.FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

			if (reactionToRemove != null)
			{
				_db.Reactions.Remove(reactionToRemove);
				await _db.SaveChangesAsync();
			}
		}

		public async Task<Dictionary<int, List<Reaction>>> GetReactionsForMessagesAsync(List<int> messageIds)
		{
			if (messageIds == null || !messageIds.Any())
				return new Dictionary<int, List<Reaction>>();

			// Убедитесь, что загружаются актуальные данные из базы
			var reactions = await _db.Reactions
				.Include(r => r.User)
				.Where(r => messageIds.Contains(r.MessageId))
				.ToListAsync();

			return reactions
				.GroupBy(r => r.MessageId)
				.ToDictionary(g => g.Key, g => g.ToList());
		}
		public async Task AddStickerMessageAsync(string sticer, int senderId, int receiverId)
		{
			var message = new Message
			{
				StickerId = sticer,
				SenderId = senderId,
				ReceiverId = receiverId,
				MessageType = "Sticer",
			};
			await _db.Messages.AddAsync(message);
			await _db.SaveChangesAsync();
		}

		public async Task UpdateMessageAsync(Message message)
		{
			_db.Messages.Update(message);
			await _db.SaveChangesAsync();
		}
		public async Task<Message> GetLastMessageForUserAsync(int userId)
		{
			return await _db.Messages
				.Where(m => m.SenderId == userId)
				.OrderByDescending(m => m.Timestamp)
				.FirstOrDefaultAsync();
		}
		public async Task<int> AddReplyMessageAndGetIdAsync(string text, int senderId, int receiverId, int? replyToMessageId)
		{
			var message = new Message
			{
				ContentMess = text,
				SenderId = senderId,
				ReceiverId = receiverId,
				MessageType = "Text",
				Timestamp = DateTime.Now,
				ReplyToMessageId = replyToMessageId
			};

			await _db.Messages.AddAsync(message);
			await _db.SaveChangesAsync();

			// Возвращаем ID сохраненного сообщения
			return message.MessageId;
		}
		public async Task<IEnumerable<Message>> GetAllMessagesAsync(int senderId, int receiverId)
		{
			var messages = await _db.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.MessageReactions) // Убедитесь, что это есть
					.ThenInclude(r => r.User)
				.Include(m => m.ReplyToMessage)
					.ThenInclude(rm => rm.Sender)
				.Where(m => m.SenderId == senderId && m.ReceiverId == receiverId ||
						   m.SenderId == receiverId && m.ReceiverId == senderId)
				.OrderByDescending(m => m.Timestamp)
				.Take(100)
				.OrderBy(m => m.Timestamp)
				.ToListAsync();

			foreach (var message in messages)
			{
				// Убедитесь, что Reactions заполняется из MessageReactions
				if (message.MessageReactions != null && message.MessageReactions.Any())
				{
					message.Reactions = new ObservableCollection<Reaction>(message.MessageReactions);
				}
				else
				{
					message.Reactions = new ObservableCollection<Reaction>();
				}
			}

			return messages;
		}
	}
}
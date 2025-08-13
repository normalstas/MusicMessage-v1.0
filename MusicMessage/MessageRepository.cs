using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MusicMessage.Models;
namespace MusicMessage
{
    public interface IMessageRepository
    {
        Task<IEnumerable<Message>> GetAllMessagesAsync(int senderId, int receiverId);
		Task AddTextMessageAsync(string text, int senderId, int receiverId);
		Task AddVoiceMessageAsync(string audioPath, TimeSpan duration, int senderId, int receiverId);
		Task AddStickerMessageAsync(string sticer, int senderId, int receiverId);
		Task AddReactionAsync(int messageId, string emoji);
		Task UpdateMessageAsync(Message message);
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
				AudioPath = audioPath,
				Duration = duration,
				SenderId = senderId,
				ReceiverId = receiverId,
				MessageType = "Voice",
				Timestamp = DateTime.Now
			};
			await _db.Messages.AddAsync(message);
			await _db.SaveChangesAsync();
		}

		public async Task AddReactionAsync(int messageId, string emoji)
		{
			var message = await _db.Messages.FindAsync(messageId);
			if (message != null)
			{
				message.Reactions = string.IsNullOrEmpty(message.Reactions)
					? emoji
					: $"{message.Reactions},{emoji}";
				await _db.SaveChangesAsync();
			}
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

		public async Task<IEnumerable<Message>> GetAllMessagesAsync(int senderId, int receiverId)
		{
			return await _db.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Where(m =>
					(m.SenderId == senderId && m.ReceiverId == receiverId) ||
					(m.SenderId == receiverId && m.ReceiverId == senderId))
				.OrderBy(m => m.Timestamp)
				.ToListAsync();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MusicMessage.Models;
namespace MusicMessage.ViewModels
{
	public class ChatViewModel : INotifyPropertyChanged
	{
		private readonly IMessageRepository _messageRepository;
		private string _messageText;
		public int CurrentUserId { get; set; } = 2;
		private int _currentReceiverId = 1; 

		public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();
		public string MessageText
		{
			get => _messageText;
			set
			{
				_messageText = value;
				OnPropertyChanged();
				(SendTextCommand as RelayCommand)?.NotifyCanExecuteChanged();
			}
		}


		public ICommand SendTextCommand { get; }
		public ICommand SendVoiceCommand { get; }
		public ICommand PlayVoiceMessageCommand { get; }

		public ChatViewModel(IMessageRepository messageRepository)
		{
			_messageRepository = messageRepository;

			// Инициализация команд
			SendTextCommand = new RelayCommand(SendTextMessage, CanSend);
			SendVoiceCommand = new RelayCommand(SendVoiceMessage);
			PlayVoiceMessageCommand = new RelayCommand<Message>(PlayVoiceMessage);

			// Загрузка сообщений при старте
			LoadMessages();
		}
		private bool CanSend() => !string.IsNullOrWhiteSpace(MessageText);

		private async void SendTextMessage()
		{
			if (string.IsNullOrWhiteSpace(MessageText))
				return;

			try
			{
				// Создаем сообщение ДО отправки, чтобы сохранить текст
				var newMessage = new Message
				{
					ContentMess = MessageText, // Сохраняем текст перед очисткой
					SenderId = CurrentUserId,
					ReceiverId = _currentReceiverId,
					Timestamp = DateTime.Now,
					MessageType = "Text",
					Sender = new User { UserName = "Вы" } // Или реальные данные из БД
				};

				// Отправляем в БД
				await _messageRepository.AddTextMessageAsync(
					MessageText,
					CurrentUserId,
					_currentReceiverId
				);

				// Очищаем поле ввода
				MessageText = string.Empty;

				// Добавляем в коллекцию
				newMessage.CurrentUserId = CurrentUserId;
				Messages.Add(newMessage);

				
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка отправки: {ex.Message}");
			}
		}



		private async void SendVoiceMessage()
		{
			try
			{
				// Здесь логика записи голоса (реализуйте отдельно)
				//var audioPath = await RecordVoiceAsync();
				//var duration = GetAudioDuration(audioPath);

				//await _messageRepository.AddVoiceMessageAsync(audioPath, duration, CurrentUserId, _currentReceiverId);
				//await LoadMessages();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка записи: {ex.Message}");
			}
		}

		

		private void PlayVoiceMessage(Message message)
		{
			if (message?.MessageType != "Voice") return;

			try
			{
				var player = new MediaPlayer();
				player.Open(new Uri(message.AudioPath, UriKind.RelativeOrAbsolute));
				player.Play();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка воспроизведения: {ex.Message}");
			}
		}

		private async Task LoadMessages()
		{
			var messages = await _messageRepository.GetAllMessagesAsync(CurrentUserId, _currentReceiverId);
			Messages.Clear();
			foreach (var msg in messages.OrderBy(m => m.Timestamp))
			{
				msg.CurrentUserId = CurrentUserId; // Критически важно!
				Messages.Add(msg);
			}
			// Убрали ScrollToEnd отсюда
		}


		

		// Вспомогательный метод для поиска ScrollViewer внутри ListView
		private static ScrollViewer GetScrollViewer(DependencyObject depObj)
		{
			if (depObj is ScrollViewer scrollViewer)
				return scrollViewer;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				var child = VisualTreeHelper.GetChild(depObj, i);
				var result = GetScrollViewer(child);
				if (result != null)
					return result;
			}
			return null;
		}



		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string propertyname = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));

	}
}

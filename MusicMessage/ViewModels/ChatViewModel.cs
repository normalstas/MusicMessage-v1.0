using System;
using System.Collections.Generic;
using System.IO;
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
using MusicMessage.ClassHelp;
using MusicMessage.Models;
using System.Diagnostics;
using NAudio.Wave;
using System.Globalization;
using Microsoft.Win32;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using MusicMessage.UserCtrls;

using MusicMessage.Repository;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media.Imaging;
namespace MusicMessage.ViewModels
{
	public class ChatViewModel : INotifyPropertyChanged
	{
		public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();
		private readonly IMessageRepository _messageRepository;
		private readonly IAuthService _authService; // Добавляем сервис аутентификации
		private string _messageText;
		private readonly VoiceRecorder _voiceRecorder = new VoiceRecorder();
		private Message _currentPlayingMessage;
		private readonly DispatcherTimer _recordingTimer;
		private List<double> _recordingWaveformData = new List<double>();
		private bool _isPreviewPlaying;
		private MediaPlayer _previewPlayer;
		private DispatcherTimer _previewTimer;
		public event Action ScrollToLastRequested;
		private ChatHeaderViewModel _chatHeader;
		private DispatcherTimer _typingTimer;
		public ChatHeaderViewModel ChatHeader
		{
			get => _chatHeader;
			set
			{
				if (_chatHeader != value)
				{
					_chatHeader = value;
					OnPropertyChanged(nameof(ChatHeader));
				}
			}
		}
		[NotMapped]
		public bool IsPreviewPlaying
		{
			get => _isPreviewPlaying;
			set
			{
				_isPreviewPlaying = value;
				OnPropertyChanged();
			}
		}
		public List<double> RecordingWaveformData
		{
			get => _recordingWaveformData;
			set
			{
				_recordingWaveformData = value;
				OnPropertyChanged();
			}
		}
		private bool _isRecordingStopped;
		public bool IsRecordingStopped
		{
			get => _isRecordingStopped;
			set
			{
				_isRecordingStopped = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsRecordingOrStopped));
				OnPropertyChanged(nameof(ShowTextInput));
				OnPropertyChanged(nameof(ShowVoiceControls));
			}
		}
		private Message _selectedMessageForReply;
		public Message SelectedMessageForReply
		{
			get => _selectedMessageForReply;
			set
			{
				_selectedMessageForReply = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsReplying));
				OnPropertyChanged(nameof(ReplyPreviewText));
			}
		}
		public bool IsReplying => SelectedMessageForReply != null;
		public string ReplyPreviewText => IsReplying ?
	$"Ответ на: {SelectedMessageForReply.Sender.UserName}: {GetPreviewText(SelectedMessageForReply)}" :
	string.Empty;
		private string GetPreviewText(Message message)
		{
			if (message.IsVoiceMessage) return "🎤 Голосовое сообщение";
			if (!string.IsNullOrEmpty(message.StickerId)) return "🖼️ Стикер";
			return message.ContentMess.Length > 30 ?
				message.ContentMess.Substring(0, 30) + "..." :
				message.ContentMess;
		}

		private byte[] _recordedAudioData;
		private TimeSpan _recordedDuration;
		[NotMapped]
		public bool IsRecordingOrStopped => IsRecording || IsRecordingStopped;

		[NotMapped]
		public bool ShowTextInput => !IsRecordingOrStopped;

		[NotMapped]
		public bool ShowVoiceControls => !IsRecordingStopped;
		private DispatcherTimer _waveformUpdateTimer;
		[NotMapped]
		public int CurrentUserId => _authService.CurrentUser?.UserId ?? 0;
		private int _currentReceiverId;
		public int CurrentReceiverId
		{
			get => _currentReceiverId;
			set
			{
				if (_currentReceiverId == value) return;

				_currentReceiverId = value;
				OnPropertyChanged();

				// Сбрасываем шапку перед загрузкой новых сообщений
				Application.Current.Dispatcher.Invoke(() =>
				{
					ChatHeader.OtherUser = null;
					ChatHeader.AvatarImage = null;
				});

				// Загружаем сообщения только если ID валидный
				if (_currentReceiverId > 0)
				{
					_ = LoadMessagesAsync();
				}
			}
		}
		[NotMapped]
		public bool ShowSendTextButton => ShowTextInput && !string.IsNullOrWhiteSpace(MessageText) && !IsEditing;

		public string MessageText
		{
			get => _messageText;
			set
			{
				_messageText = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ShowSendTextButton));
				OnPropertyChanged(nameof(ShowRecordingButton)); // Добавьте эту строку
				(SendTextCommand as RelayCommand)?.NotifyCanExecuteChanged();
				(ConfirmEditCommand as RelayCommand)?.NotifyCanExecuteChanged();
			}
		}
		private bool _isRecording;
		public bool IsRecording
		{
			get => _isRecording;
			set
			{
				_isRecording = value;
				OnPropertyChanged();
			}
		}

		private TimeSpan _recordingDuration;
		public TimeSpan RecordingDuration
		{
			get => _recordingDuration;
			set
			{
				_recordingDuration = value;
				OnPropertyChanged();
			}
		}
		private Message _editingMessage;
		public Message EditingMessage
		{
			get => _editingMessage;
			set
			{
				_editingMessage = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsEditing));

				// Копируем текст редактируемого сообщения в поле ввода
				MessageText = value?.ContentMess ?? string.Empty;

				// Обновляем видимость всех кнопок
				OnPropertyChanged(nameof(ShowSendTextButton));
				OnPropertyChanged(nameof(ShowRecordingButton)); // Добавьте эту строку

				// Уведомляем команду об изменении состояния
				(ConfirmEditCommand as RelayCommand)?.NotifyCanExecuteChanged();
			}
		}
		[NotMapped]
		public bool ShowRecordingButton => ShowVoiceControls && !IsEditing;
		// Свойство для проверки, идет ли редактирование
		[NotMapped]
		public bool IsEditing => EditingMessage != null;
		public ICommand SendTextCommand { get; }
		public ICommand SendRecordedVoiceCommand { get; }
		public ICommand CancelRecordingCommand { get; }
		public ICommand ReRecordCommand { get; }
		public ICommand StartRecordingCommand { get; } // Для начала записи
		public ICommand StopRecordingCommand { get; }  // Для остановки и отправки
		public ICommand PlayVoiceMessageCommand { get; }
		public ICommand PreviewRecordedVoiceCommand { get; }
		public ICommand StartReplyCommand { get; }
		public ICommand CancelReplyCommand { get; }
		public ICommand DeleteMessageForMeCommand { get; }
		public ICommand DeleteMessageForEveryoneCommand { get; }
		public ICommand DeleteMessageForReceiverCommand { get; }
		public ICommand CopyTextCommand { get; }
		public ICommand CopyTimeCommand { get; }
		public ICommand SaveVoiceMessageCommand { get; }
		public ICommand NavigateToReplyMessageCommand { get; }
		public ICommand StartEditCommand { get; private set; }
		public ICommand ConfirmEditCommand { get; private set; }
		public ICommand CancelEditCommand { get; private set; }
		public ICommand ToggleReactionCommand { get; private set; }
		public ICommand LoadedCommand { get; }
		public ICommand BackCommand { get; }
		public ICommand VoiceCallCommand { get; }
		public ICommand VideoCallCommand { get; }
		public ICommand ChatMenuCommand { get; }
		public ICommand OpenProfileCommand { get; }
		public ChatViewModel(IMessageRepository messageRepository, IAuthService authService)
		{

			_messageRepository = messageRepository;
			_authService = authService;
			if (!_authService.IsLoggedIn)
			{
				MessageBox.Show("Пользователь не авторизован");
				// Можно выбросить исключение или обработать эту ситуацию
				return;
			}
			InitializeVoiceMessagesDirectory();
			// Инициализация команд
			SendTextCommand = new RelayCommand(SendTextMessage, CanSend);
			SendRecordedVoiceCommand = new RelayCommand(SendRecordedVoice);
			CancelRecordingCommand = new RelayCommand(CancelRecording);
			ReRecordCommand = new RelayCommand(ReRecord);
			StartRecordingCommand = new RelayCommand(StartRecording);
			StopRecordingCommand = new RelayCommand(StopRecording);
			PlayVoiceMessageCommand = new RelayCommand<Message>(PlayVoiceMessage);
			PreviewRecordedVoiceCommand = new RelayCommand(PreviewRecordedVoice);
			StartReplyCommand = new RelayCommand<Message>(StartReply);
			CancelReplyCommand = new RelayCommand(CancelReply);
			DeleteMessageForMeCommand = new RelayCommand<Message>(DeleteMessageForMe);
			DeleteMessageForEveryoneCommand = new RelayCommand<Message>(DeleteMessageForEveryone);
			DeleteMessageForReceiverCommand = new RelayCommand<Message>(DeleteMessageForReceiver);
			CopyTextCommand = new RelayCommand<string>(CopyText);
			CopyTimeCommand = new RelayCommand<DateTime>(CopyTime);
			SaveVoiceMessageCommand = new RelayCommand<Message>(SaveVoiceMessage);
			NavigateToReplyMessageCommand = new RelayCommand<Message>(NavigateToReplyMessage);
			StartEditCommand = new RelayCommand<Message>(StartEdit);
			ConfirmEditCommand = new RelayCommand(ConfirmEdit, CanConfirmEdit);
			CancelEditCommand = new RelayCommand(CancelEdit);
			ToggleReactionCommand = new RelayCommand<object>(ToggleReaction);
			LoadedCommand = new RelayCommand(async () => await LoadMessagesAsync());
			BackCommand = new RelayCommand(GoBack);
			VoiceCallCommand = new RelayCommand(StartVoiceCall);
			VideoCallCommand = new RelayCommand(StartVideoCall);
			ChatMenuCommand = new RelayCommand(ShowChatMenu);
			OpenProfileCommand = new RelayCommand(OpenProfile);
			_recordingTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1)
			};
			_recordingTimer.Tick += (s, e) => RecordingDuration += TimeSpan.FromSeconds(1);
			// Подписка на события VoiceRecorder
			_voiceRecorder.RecordingStopped += OnRecordingStopped;
			// Загрузка сообщений при старте
			Application.Current.Exit += (s, e) => Cleanup();
			_voiceRecorder.AudioDataAvailable += OnAudioDataAvailable;
			_waveformUpdateTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(100)
			};
			_waveformUpdateTimer.Tick += UpdateRecordingWaveform;
			PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(IsRecording) ||
					e.PropertyName == nameof(IsRecordingStopped) ||
					e.PropertyName == nameof(MessageText))
				{
					OnPropertyChanged(nameof(ShowSendTextButton));
				}
			};
			ChatHeader = new ChatHeaderViewModel();
			_typingTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(3)
			};
			_typingTimer.Tick += (s, e) => ClearTypingStatus();
			// Инициализация медиаплеера для предпросмотра
			_previewPlayer = new MediaPlayer();
			_previewTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(100)
			};
			_previewTimer.Tick += (s, e) => UpdatePreviewProgress();

			_previewPlayer.MediaEnded += (s, e) => StopPreview();
			Task.Run(async () => await LoadMessagesAsync());
		}
		

		private bool CanSend() => !string.IsNullOrWhiteSpace(MessageText);
		public async Task LoadMessagesForCurrentReceiverAsync()
		{
			if (_currentReceiverId == 0) return;
			await LoadMessagesAsync();
		}
		private void OpenProfile()
		{
			if (ChatHeader?.OtherUser != null)
			{
				var navigationVM = App.ServiceProvider.GetService<NavigationViewModel>();
				navigationVM?.ShowFriendProfileCommand.Execute(ChatHeader.OtherUser.UserId);
			}
		}
		private async void SendTextMessage()
		{
			if (IsEditing)
			{
				ConfirmEdit();
				return;
			}

			if (string.IsNullOrWhiteSpace(MessageText))
				return;

			try
			{
				var chatRepository = App.ServiceProvider.GetService<IChatRepository>();

				// СОЗДАЕМ ЧАТ-ПРЕВЬЮ ДЛЯ ОБОИХ УЧАСТНИКОВ
				await chatRepository.CreateChatPreviewAsync(CurrentUserId, _currentReceiverId);

				// Сначала сохраняем сообщение в БД чтобы получить MessageId
				var messageId = await _messageRepository.AddReplyMessageAndGetIdAsync(
					MessageText,
					CurrentUserId,
					_currentReceiverId,
					SelectedMessageForReply?.MessageId
				);

				// Теперь загружаем полное сообщение из БД с полученным ID
				var newMessage = await _messageRepository.GetMessageByIdAsync(messageId);

				if (newMessage == null)
				{
					MessageBox.Show("Ошибка: сообщение не найдено после сохранения");
					return;
				}

				// Инициализируем свойства для UI
				newMessage.CurrentUserId = CurrentUserId;
				newMessage.Sender = new User { UserName = "Вы" };

				// ВАЖНО: Инициализируем коллекцию и команду
				if (newMessage.Reactions == null)
				{
					newMessage.Reactions = new ObservableCollection<Reaction>();
				}

				newMessage.ToggleReactionCommand = new RelayCommand<object[]>(ToggleReaction);

				// ВАЖНО: Загружаем полные данные о сообщении-ответе, если есть
				if (newMessage.ReplyToMessageId.HasValue && newMessage.ReplyToMessage == null)
				{
					newMessage.ReplyToMessage = await _messageRepository.GetMessageByIdAsync(
						newMessage.ReplyToMessageId.Value);
				}

				MessageText = string.Empty;
				SelectedMessageForReply = null;

				// ДОБАВЛЯЕМ сообщение в коллекцию и ОБНОВЛЯЕМ UI
				Messages.Add(newMessage);
				ScrollToLastRequested?.Invoke();

				// ОБНОВЛЯЕМ счетчики непрочитанных для ОБОИХ участников
				await UpdateUnreadCountAsync(_currentReceiverId, CurrentUserId);
				await UpdateUnreadCountAsync(CurrentUserId, _currentReceiverId);

				// ОБНОВЛЯЕМ список чатов для ОБОИХ участников (если они онлайн)
				await UpdateChatsListForBothUsers();

			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка отправки: {ex.Message}");
			}
		}
		private async Task UpdateChatsListForBothUsers()
		{
			try
			{
				// Обновляем список чатов для текущего пользователя
				var chatsListVM = App.ServiceProvider.GetService<ChatsListViewModel>();
				if (chatsListVM != null)
				{
					await chatsListVM.LoadChatsAsync();
				}

				// Здесь можно добавить логику для обновления списка чатов
				// у второго пользователя, если он онлайн (через SignalR или другой механизм)
				// Пока просто обновляем данные в базе для второго пользователя
				var chatRepository = App.ServiceProvider.GetService<IChatRepository>();
				if (chatRepository != null)
				{
					await chatRepository.UpdateAllChatsLastMessagesAsync(_currentReceiverId);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Ошибка обновления списка чатов: {ex.Message}");
			}
		}
		private void StartReply(Message message)
		{
			SelectedMessageForReply = message;
		}

		private void CancelReply()
		{
			SelectedMessageForReply = null;
		}
		private async void DeleteMessageForMe(Message message)
		{
			if (message == null || message.SenderId != CurrentUserId) return;

			try
			{
				// Обновляем в базе данных
				await _messageRepository.DeleteMessageForMeAsync(message.MessageId, CurrentUserId);

				// Обновляем локальную копию
				message.IsDeletedForSender = true;

				// Уведомляем UI об изменениях
				var index = Messages.IndexOf(message);
				if (index != -1)
				{
					Messages.RemoveAt(index);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка удаления: {ex.Message}");
			}
		}

		private async void DeleteMessageForEveryone(Message message)
		{
			if (message == null || message.SenderId != CurrentUserId) return;

			try
			{
				// Обновляем в базе данных
				await _messageRepository.DeleteMessageForEveryoneAsync(message.MessageId);

				// Обновляем локальную копию
				message.IsDeletedForEveryone = true;

				// Удаляем из коллекции
				Messages.Remove(message);

				// Обновляем список чатов
				var chatsListVM = App.ServiceProvider.GetService<ChatsListViewModel>();
				if (chatsListVM != null)
				{
					await chatsListVM.LoadChatsAsync();
				}

				// Если это голосовое сообщение, удаляем файл
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
			catch (DbUpdateConcurrencyException ex)
			{
				// Сообщение уже было удалено кем-то другим
				MessageBox.Show("Сообщение уже было удалено");
				Messages.Remove(message); // Все равно удаляем из локальной коллекции
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка удаления: {ex.Message}");
			}
		}

		private async void DeleteMessageForReceiver(Message message)
		{
			if (message == null || message.ReceiverId != CurrentUserId) return;

			try
			{
				// Обновляем в базе данных
				await _messageRepository.DeleteMessageForReceiverAsync(message.MessageId, CurrentUserId);

				// Удаляем из коллекции
				Messages.Remove(message);

				Debug.WriteLine($"Message {message.MessageId} deleted for receiver");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка удаления: {ex.Message}");
			}
		}

		private async Task DeleteAllMessages()
		{
			if (_currentReceiverId == 0) return;

			var result = MessageBox.Show("Вы уверены, что хотите удалить всю переписку для всех участников?",
				"Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

			if (result == MessageBoxResult.Yes)
			{
				try
				{
					using var context = new MessangerBaseContext();

					// Удаляем все сообщения
					await _messageRepository.DeleteAllMessagesForEveryoneAsync(CurrentUserId, _currentReceiverId);

					// Очищаем локальную коллекцию
					Messages.Clear();

					// Обновляем список чатов
					var chatsListVM = App.ServiceProvider.GetService<ChatsListViewModel>();
					if (chatsListVM != null)
					{
						await chatsListVM.LoadChatsAsync();
					}

					MessageBox.Show("Переписка удалена");
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка удаления переписки: {ex.Message}");
				}
			}
		}
		private void CopyText(string text)
		{
			try
			{
				Clipboard.SetText(text);
			}
			catch { }
		}

		private void CopyTime(DateTime timestamp)
		{
			try
			{
				Clipboard.SetText(timestamp.ToString("HH:mm"));
			}
			catch { }
		}
		private void StartEdit(Message message)
		{
			if (message?.IsEditable == true)
			{
				EditingMessage = message;
			}
		}

		// Метод для подтверждения редактирования
		private async void ConfirmEdit()
		{
			if (EditingMessage == null || string.IsNullOrWhiteSpace(MessageText))
				return;

			try
			{
				// Сохраняем ссылку на редактируемое сообщение
				var messageId = EditingMessage.MessageId;
				var originalTimestamp = EditingMessage.Timestamp;

				// Обновляем только текст в базе данных
				await _messageRepository.UpdateMessageTextAsync(messageId, MessageText);

				// Находим сообщение в коллекции и обновляем его
				var messageToUpdate = Messages.FirstOrDefault(m => m.MessageId == messageId);
				if (messageToUpdate != null)
				{
					// Обновляем только текст, время оставляем прежним
					messageToUpdate.ContentMess = MessageText;

					// Принудительно обновляем UI
					var index = Messages.IndexOf(messageToUpdate);
					if (index != -1)
					{
						// Временное удаление и добавление для обновления UI
						Messages.RemoveAt(index);

						// Создаем новое сообщение с обновленным текстом но старым временем
						var updatedMessage = new Message
						{
							MessageId = messageToUpdate.MessageId,
							ContentMess = MessageText, // Новый текст
							SenderId = messageToUpdate.SenderId,
							ReceiverId = messageToUpdate.ReceiverId,
							Timestamp = originalTimestamp, // Старое время
							MessageType = messageToUpdate.MessageType,
							AudioPath = messageToUpdate.AudioPath,
							Duration = messageToUpdate.Duration,
							StickerId = messageToUpdate.StickerId,
							Reactions = messageToUpdate.Reactions,
							ReplyToMessageId = messageToUpdate.ReplyToMessageId,
							ReplyToMessage = messageToUpdate.ReplyToMessage,
							Sender = messageToUpdate.Sender,
							Receiver = messageToUpdate.Receiver,
							CurrentUserId = messageToUpdate.CurrentUserId
						};

						Messages.Insert(index, updatedMessage);
					}
				}

				CancelEdit();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка редактирования: {ex.Message}");
			}
		}


		// Метод проверки возможности редактирования
		private bool CanConfirmEdit()
		{
			return !string.IsNullOrWhiteSpace(MessageText) &&
				   EditingMessage != null;
			// Убрали проверку на изменение текста, чтобы кнопка была всегда активна
			// EditingMessage.ContentMess != MessageText;
		}

		// Метод для отмены редактирования
		private void CancelEdit()
		{
			EditingMessage = null;
			MessageText = string.Empty;
			// Также обновляем видимость кнопок после отмены редактирования
			OnPropertyChanged(nameof(ShowRecordingButton));
		}

		private void SaveVoiceMessage(Message message)
		{
			if (!message.IsVoiceMessage || string.IsNullOrEmpty(message.AudioPath)) return;

			try
			{
				var saveDialog = new SaveFileDialog
				{
					Filter = "Audio files (*.wav)|*.wav|All files (*.*)|*.*",
					FileName = $"voice_message_{message.Timestamp:yyyyMMdd_HHmmss}.wav"
				};

				if (saveDialog.ShowDialog() == true)
				{
					var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, message.AudioPath);
					if (File.Exists(sourcePath))
					{
						File.Copy(sourcePath, saveDialog.FileName, true);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка сохранения: {ex.Message}");
			}
		}

		private void NavigateToReplyMessage(Message replyMessage)
		{
			if (replyMessage?.ReplyToMessageId == null) return;

			Debug.WriteLine($"Navigating to message: {replyMessage.ReplyToMessageId}");

			// Ищем сообщение в текущей коллекции
			var targetMessage = Messages.FirstOrDefault(m => m.MessageId == replyMessage.ReplyToMessageId);

			if (targetMessage != null)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					// Ищем ChatUserCtrl во всех открытых окнах
					foreach (Window window in Application.Current.Windows)
					{
						var chatControl = VisualTreeHelperExtensions.FindVisualChildren<ChatUserCtrl>(window).FirstOrDefault();
						if (chatControl != null)
						{
							chatControl.ScrollToMessage(targetMessage);
							break;
						}
					}
				});
			}
			else
			{
				Debug.WriteLine("Target message not found in current chat");
				MessageBox.Show("Сообщение не найдено в текущем чате");
			}
		}

		// Вспомогательный метод для поиска дочерних элементов
		private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
		{
			if (depObj != null)
			{
				for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
				{
					DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
					if (child != null && child is T)
					{
						yield return (T)child;
					}

					foreach (T childOfChild in FindVisualChildren<T>(child))
					{
						yield return childOfChild;
					}
				}
			}
		}




		private void InitializeVoiceMessagesDirectory()
		{
			var voiceMessagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VoiceMessages");
			Directory.CreateDirectory(voiceMessagesDir);
		}
		private void OnRecordingStopped(TimeSpan duration)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				IsRecording = false;
				RecordingDuration = duration;
			});
		}
		private void UpdateRecordingWaveform(object sender, EventArgs e)
		{
			// Генерируем случайные данные для визуализации во время записи
			var random = new Random();
			var newData = Enumerable.Range(0, 30)
								   .Select(_ => (double)random.Next(5, 25))
								   .ToList();

			RecordingWaveformData = newData;
		}
		private void OnAudioDataAvailable(byte[] buffer)
		{
			if (buffer == null || buffer.Length == 0)
				return;


			short[] samples = new short[buffer.Length / 2];
			Buffer.BlockCopy(buffer, 0, samples, 0, buffer.Length);


			short maxAmplitude = samples.Max(s => s > 0 ? s : (short)-s);


			double height = maxAmplitude / 32768.0 * 30;

			Application.Current.Dispatcher.Invoke(() =>
			{
				if (RecordingWaveformData.Count > 30)
					RecordingWaveformData.RemoveAt(0);

				RecordingWaveformData.Add(height);
			});
		}
		private void PreviewRecordedVoice()
		{
			if (_recordedAudioData == null || _recordedAudioData.Length == 0)
				return;

			try
			{
				if (IsPreviewPlaying)
				{
					// Если уже играет - останавливаем
					StopPreview();
					return;
				}

				// Сохраняем во временный файл для воспроизведения
				var tempFile = Path.GetTempFileName() + ".wav";
				File.WriteAllBytes(tempFile, _recordedAudioData);

				_previewPlayer.Open(new Uri(tempFile, UriKind.Absolute));
				_previewPlayer.Play();
				IsPreviewPlaying = true;
				_previewTimer.Start();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка воспроизведения: {ex.Message}");
			}
		}

		private void StopPreview()
		{
			_previewPlayer.Stop();
			_previewTimer.Stop();
			IsPreviewPlaying = false;

			// Удаляем временный файл
			try
			{
				if (_previewPlayer.Source != null && _previewPlayer.Source.IsFile)
				{
					File.Delete(_previewPlayer.Source.LocalPath);
				}
			}
			catch { /* Игнорируем ошибки удаления */ }
		}

		private void UpdatePreviewProgress()
		{
			// Здесь можно обновлять прогресс воспроизведения, если нужно
			// Например, для визуализации прогресса прослушивания
		}


		private async void SendRecordedVoice()
		{
			if (_recordedAudioData != null && _recordedAudioData.Length > 0)
			{
				await SendVoiceMessageAsync(_recordedAudioData, _recordedDuration);
				ResetRecordingState();
				ScrollToLastRequested?.Invoke();
			}
		}

		private void CancelRecording()
		{
			_recordedAudioData = null;
			_recordedDuration = TimeSpan.Zero;
			ResetRecordingState();
		}

		private void ReRecord()
		{
			_recordedAudioData = null;
			_recordedDuration = TimeSpan.Zero;
			IsRecordingStopped = false;
			StartRecording();
		}

		private void ResetRecordingState()
		{
			IsRecordingStopped = false;
			_recordedAudioData = null;
			_recordedDuration = TimeSpan.Zero;
			OnPropertyChanged(nameof(ShowTextInput));
			OnPropertyChanged(nameof(ShowVoiceControls));
		}


		private void StartRecording()
		{
			_voiceRecorder.StartRecording();
			IsRecording = true;
			RecordingDuration = TimeSpan.Zero;
			_waveformUpdateTimer.Start();
			_recordingTimer.Start();
		}

		private void StopRecording()
		{
			var (audioData, duration) = _voiceRecorder.StopRecording();
			IsRecording = false;
			_waveformUpdateTimer.Stop();
			_recordingTimer.Stop();

			if (audioData != null && audioData.Length > 0)
			{
				_recordedAudioData = audioData;
				_recordedDuration = duration;
				IsRecordingStopped = true;
			}
		}

		private void PlayVoiceMessage(Message message)
		{
			if (message == null) return;
			if (message?.MessageType != "Voice") return;

			try
			{
				// Остановить текущее воспроизведение, если оно есть
				if (_currentPlayingMessage != null && _currentPlayingMessage != message)
				{
					_currentPlayingMessage.IsPlaying = false;
					_currentPlayingMessage.IsPaused = false;
					_currentPlayingMessage.CurrentPlaybackTime = null; // Сбрасываем время
					_currentPlayingMessage.CleanupPlayer();
				}

				// Если сообщение уже играет и на паузе - возобновляем
				if (message.IsPlaying && message.IsPaused)
				{
					message.Player.Play();
					message.IsPaused = false;
					_currentPlayingMessage = message;
					return;
				}
				// Если сообщение играет без паузы - ставим на паузу
				else if (message.IsPlaying)
				{
					message.Player.Pause();
					message.IsPaused = true;
					return;
				}

				// Новое воспроизведение
				var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, message.AudioPath);

				if (!File.Exists(fullPath))
				{
					MessageBox.Show("Аудиофайл не найден");
					return;
				}

				message.InitializePlayer();
				message.Player.Open(new Uri(fullPath, UriKind.Absolute));

				// Обновляем таймер для отображения текущего времени
				message.PlaybackTimer.Tick += (s, e) => UpdatePlaybackPositionAndTime(message);
				message.PlaybackTimer.Start();

				message.Player.MediaEnded += (s, e) => OnPlaybackEnded(message);

				// Устанавливаем обработчик для обновления времени при изменении позиции
				message.Player.MediaOpened += (s, e) =>
				{
					// Инициализируем начальное время
					UpdatePlaybackTime(message);
				};

				message.Player.Play();
				message.IsPlaying = true;
				message.IsPaused = false;
				_currentPlayingMessage = message;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка воспроизведения: {ex.Message}");
			}
		}

		private void UpdatePlaybackPositionAndTime(Message message)
		{
			UpdatePlaybackPosition(message);
			UpdatePlaybackTime(message);
		}

		private void UpdatePlaybackTime(Message message)
		{
			if (message.Player != null && message.Player.NaturalDuration.HasTimeSpan)
			{
				// Форматируем текущее время позиции
				var currentTime = message.Player.Position;
				message.CurrentPlaybackTime = currentTime.ToString("mm\\:ss");
			}
		}

		private void OnPlaybackEnded(Message message)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				// Восстанавливаем исходную длительность после завершения
				if (message.Duration.HasValue)
				{
					message.CurrentPlaybackTime = message.Duration.Value.ToString("mm\\:ss");
				}
				else
				{
					message.CurrentPlaybackTime = null;
				}

				message.CleanupPlayer();
				_currentPlayingMessage = null;

				// Найти следующее голосовое сообщение после текущего
				PlayNextVoiceMessage(message);
			});
		}

		public void Cleanup()
		{
			// Остановка записи, если она идет
			if (IsRecording)
			{
				_voiceRecorder.StopRecording();
			}

			// Остановка воспроизведения
			if (_currentPlayingMessage != null)
			{
				_currentPlayingMessage.CleanupPlayer();
				_currentPlayingMessage = null;
			}

			// Остановка предпросмотра
			StopPreview();
			_previewPlayer.Close();

			// Остановка таймера записи
			_recordingTimer.Stop();

			// Отписка от событий
			_voiceRecorder.RecordingStopped -= OnRecordingStopped;
		}
		private void UpdatePlaybackPosition(Message message)
		{
			if (message.Player.NaturalDuration.HasTimeSpan)
			{
				message.PlaybackPosition = message.Player.Position.TotalMilliseconds /
										 message.Player.NaturalDuration.TimeSpan.TotalMilliseconds * 100;
			}
		}


		private async Task SendVoiceMessageAsync(byte[] audioData, TimeSpan duration)
		{
			try
			{
				var chatRepository = App.ServiceProvider.GetService<IChatRepository>();

				// СОЗДАЕМ ЧАТ-ПРЕВЬЮ ДЛЯ ОБОИХ УЧАСТНИКОВ
				await chatRepository.CreateChatPreviewAsync(CurrentUserId, _currentReceiverId);

				var fileName = $"voice_{DateTime.Now:yyyyMMddHHmmss}.wav";
				var voiceMessagePath = Path.Combine("VoiceMessages", fileName);

				Directory.CreateDirectory("VoiceMessages");
				File.WriteAllBytes(voiceMessagePath, audioData);

				var waveformData = await Task.Run(() => GenerateWaveformData(voiceMessagePath));

				// Сохраняем сообщение в базу
				await _messageRepository.AddVoiceMessageAsync(
					voiceMessagePath,
					duration,
					CurrentUserId,
					_currentReceiverId
				);

				// Получаем последнее сообщение для отображения в UI
				var lastMessage = await _messageRepository.GetLastMessageForUserAsync(CurrentUserId);

				if (lastMessage != null)
				{
					lastMessage.CurrentUserId = CurrentUserId;
					lastMessage.Sender = new User { UserName = "Вы" };
					lastMessage.Reactions = new ObservableCollection<Reaction>();
					lastMessage.ToggleReactionCommand = new RelayCommand<object[]>(ToggleReaction);

					if (lastMessage.IsVoiceMessage)
					{
						lastMessage.WaveformDataList = GenerateRandomWaveform();
					}

					Messages.Add(lastMessage);

					// ОБНОВЛЯЕМ счетчики непрочитанных для ОБОИХ участников
					await UpdateUnreadCountAsync(_currentReceiverId, CurrentUserId);
					await UpdateUnreadCountAsync(CurrentUserId, _currentReceiverId);

					// ОБНОВЛЯЕМ список чатов для ОБОИХ участников
					await UpdateChatsListForBothUsers();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка отправки голосового сообщения: {ex.Message}");
			}
		}
		private async void ToggleReaction(object parameter)
		{
			try
			{
				// Обрабатываем два формата параметров:
				// 1. Из контекстного меню: object[] { message, emoji }
				// 2. Из кнопки реакции: Reaction object

				Message message = null;
				string emoji = null;
				int messageId = 0;
				int userId = CurrentUserId;

				if (parameter is object[] parameters && parameters.Length >= 2)
				{
					// Вызов из контекстного меню
					message = parameters[0] as Message;
					emoji = parameters[1] as string;
					if (message != null) messageId = message.MessageId;
				}
				else if (parameter is Reaction reaction)
				{
					// Вызов из кнопки реакции
					messageId = reaction.MessageId;
					userId = reaction.UserId;
					emoji = reaction.Emoji;

					// Находим сообщение в коллекции
					message = Messages.FirstOrDefault(m => m.MessageId == reaction.MessageId);
				}

				if (messageId == 0 || string.IsNullOrEmpty(emoji)) return;

				// Проверяем, есть ли уже такая реакция от текущего пользователя
				var existingReaction = message?.Reactions?.FirstOrDefault(r =>
					r.UserId == CurrentUserId && r.Emoji == emoji);

				if (existingReaction != null)
				{
					// Удаляем реакцию
					await _messageRepository.RemoveReactionAsync(messageId, CurrentUserId);
				}
				else
				{
					// Добавляем/обновляем реакцию
					await _messageRepository.AddOrUpdateReactionAsync(messageId, CurrentUserId, emoji);
				}

				// Перезагружаем реакции из базы
				var allMessageIds = Messages.Select(m => m.MessageId).ToList();
				var reactionsDict = await GetReactionsForMessagesAsync(allMessageIds);

				await Application.Current.Dispatcher.InvokeAsync(() =>
				{
					foreach (var msg in Messages)
					{
						if (reactionsDict.TryGetValue(msg.MessageId, out var updatedReactions))
						{
							msg.Reactions = new ObservableCollection<Reaction>(updatedReactions);
						}
						else
						{
							msg.Reactions = new ObservableCollection<Reaction>();
						}
						msg.OnPropertyChanged(nameof(Message.ReactionsSummary));
						msg.OnPropertyChanged(nameof(Message.Reactions));
					}
				});
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in ToggleReaction: {ex.Message}");
				MessageBox.Show($"Ошибка при установке реакции: {ex.Message}");
			}
		}
		public async Task UpdateChatHeaderInfo()
		{
			try
			{
				// СБРАСЫВАЕМ текущие данные перед загрузкой новых
				Application.Current.Dispatcher.Invoke(() =>
				{
					ChatHeader.OtherUser = null;
					ChatHeader.AvatarImage = null;
					ChatHeader.IsOnline = false;
					ChatHeader.LastSeen = null;
					ChatHeader.TypingStatus = string.Empty;
				});

				using var context = App.ServiceProvider.GetRequiredService<IDbContextFactory<MessangerBaseContext>>().CreateDbContext();

				var otherUser = await context.Users
					.FirstOrDefaultAsync(u => u.UserId == _currentReceiverId);

				if (otherUser != null)
				{
					Application.Current.Dispatcher.Invoke(() =>
					{
						ChatHeader.OtherUser = otherUser;
						ChatHeader.IsOnline = otherUser.IsOnline;
						ChatHeader.LastSeen = otherUser.LastSeen;

						// Принудительно загружаем аватарку
						LoadAvatarForHeader(otherUser);
					});
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Ошибка обновления шапки чата: {ex.Message}");
			}
		}
		private async void LoadAvatarForHeader(User user)
		{
			if (user == null || string.IsNullOrEmpty(user.AvatarPath))
			{
				ChatHeader.AvatarImage = null;
				return;
			}

			try
			{
				var image = await LoadImageAsync(user.AvatarPath);
				Application.Current.Dispatcher.Invoke(() =>
				{
					// Проверяем, что мы все еще в том же чате
					if (ChatHeader.OtherUser?.UserId == user.UserId)
					{
						ChatHeader.AvatarImage = image;
					}
				});
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Ошибка загрузки аватарки: {ex.Message}");
				ChatHeader.AvatarImage = null;
			}
		}

		// Метод для загрузки изображения
		private async Task<BitmapImage> LoadImageAsync(string fileName)
		{
			return await Task.Run(() =>
			{
				try
				{
					if (string.IsNullOrEmpty(fileName))
						return null;

					var possiblePaths = new[]
					{
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars", fileName),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Covers", fileName),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
				fileName
			};

					foreach (var fullPath in possiblePaths)
					{
						if (File.Exists(fullPath))
						{
							var bitmap = new BitmapImage();
							bitmap.BeginInit();
							bitmap.UriSource = new Uri(fullPath);
							bitmap.CacheOption = BitmapCacheOption.OnLoad;
							bitmap.EndInit();
							bitmap.Freeze();
							return bitmap;
						}
					}

					return null;
				}
				catch
				{
					return null;
				}
			});
		}


		// Метод для показа статуса "печатает"
		public void SetTypingStatus(string userName)
		{
			ChatHeader.TypingStatus = $"{userName} печатает...";
			_typingTimer.Stop();
			_typingTimer.Start();
		}

		private void ClearTypingStatus()
		{
			ChatHeader.TypingStatus = string.Empty;
			_typingTimer.Stop();
		}
		private List<double> GenerateRandomWaveform()
		{
			var random = new Random();
			return Enumerable.Range(0, 30)
						   .Select(_ => (double)random.Next(5, 25))
						   .ToList();
		}
		private async Task LoadMessagesAsync()
		{
			try
			{
				using var context = App.ServiceProvider.GetRequiredService<IDbContextFactory<MessangerBaseContext>>().
			CreateDbContext();
				var messageRepository = new MessageRepository(App.ServiceProvider.
					GetRequiredService<IDbContextFactory<MessangerBaseContext>>());

				// 1. СНАЧАЛА получаем непрочитанные сообщения ДО загрузки чата
				var unreadMessagesToUpdate = await context.Messages
					.Where(m => m.SenderId == _currentReceiverId &&
							  m.ReceiverId == CurrentUserId &&
							  !m.IsRead &&
							  !m.IsDeletedForEveryone &&
							  !(m.IsDeletedForReceiver && m.ReceiverId == CurrentUserId)) // ДОБАВЬТЕ эту проверку
					.ToListAsync();
				// 2. Помечаем как прочитанные
				if (unreadMessagesToUpdate.Count > 0)
				{
					foreach (var msg in unreadMessagesToUpdate)
					{
						msg.IsRead = true;
					}
					await context.SaveChangesAsync();
				}

				// 3. ОБНОВЛЯЕМ СЧЕТЧИК в ChatPreviews
				await UpdateUnreadCountAsync(CurrentUserId, _currentReceiverId);

				// 4. ТЕПЕРЬ загружаем сообщения для отображения

				var messages = await context.Messages
		   .Include(m => m.Sender)
		   .Include(m => m.Receiver)
		   .Include(m => m.MessageReactions) // ВАЖНО: включаем реакции
			   .ThenInclude(r => r.User)     // и пользователей реакций
		   .Include(m => m.ReplyToMessage)
			   .ThenInclude(rm => rm.Sender)
		   .Where(m => (m.SenderId == CurrentUserId && m.ReceiverId == _currentReceiverId) ||
					  (m.SenderId == _currentReceiverId && m.ReceiverId == CurrentUserId))
		   .Where(m => !m.IsDeletedForEveryone &&
					  !(m.IsDeletedForSender && m.SenderId == CurrentUserId) &&
					  !(m.IsDeletedForReceiver && m.ReceiverId == CurrentUserId))
		   .OrderBy(m => m.Timestamp)
		   .AsNoTracking()
		   .ToListAsync();
				var messageIds = messages.Select(m => m.MessageId).ToList();
				var reactionsDict = await GetReactionsForMessagesAsync(messageIds);
				// 5. Отображаем сообщения в UI
				await Application.Current.Dispatcher.InvokeAsync(() =>
				{
					Messages.Clear();
					foreach (var msg in messages)
					{
						msg.MessageType = string.IsNullOrEmpty(msg.AudioPath) ? "Text" : "Voice";
						msg.CurrentUserId = CurrentUserId;
						if (reactionsDict.TryGetValue(msg.MessageId, out var messageReactions))
						{
							msg.Reactions = new ObservableCollection<Reaction>(messageReactions);
						}
						else
						{
							msg.Reactions = new ObservableCollection<Reaction>();
						}
						if (msg.Sender == null && msg.SenderId > 0)
						{
							msg.Sender = new User { UserName = "Unknown Sender" };
						}

						if (msg.Receiver == null && msg.ReceiverId > 0)
						{
							msg.Receiver = new User { UserName = "Unknown Receiver" };
						}

						if (msg.Reactions == null)
						{
							msg.Reactions = new ObservableCollection<Reaction>();
						}

						if (msg.ToggleReactionCommand == null)
						{
							msg.ToggleReactionCommand = new RelayCommand<object[]>(ToggleReaction);
						}

						if (msg.IsVoiceMessage)
						{
							msg.WaveformDataList = GenerateRandomWaveform();
						}

						Messages.Add(msg);
					}
					ScrollToLastRequested?.Invoke();
				});
				await UpdateChatHeaderInfo();

				// Принудительно обновляем привязки
				Application.Current.Dispatcher.Invoke(() =>
				{
					OnPropertyChanged(nameof(ChatHeader));
					ChatHeader.OnPropertyChanged(nameof(ChatHeader.AvatarImage));
					ChatHeader.OnPropertyChanged(nameof(ChatHeader.OtherUser));
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки сообщений: {ex.Message}");
			}
		}
		
		private async Task<Dictionary<int, List<Reaction>>> GetReactionsForMessagesAsync(List<int> messageIds)
		{
			if (messageIds == null || !messageIds.Any())
				return new Dictionary<int, List<Reaction>>();

			try
			{
				using var context = new MessangerBaseContext();

				var reactions = await context.Reactions
					.Include(r => r.User)
					.Where(r => messageIds.Contains(r.MessageId))
					.AsNoTracking()
					.ToListAsync();
				return reactions
					.GroupBy(r => r.MessageId)
					.ToDictionary(g => g.Key, g => g.ToList());
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки реакций: {ex.Message}");
				return new Dictionary<int, List<Reaction>>();
			}
		}
		private async Task UpdateUnreadCountAsync(int userId, int otherUserId)
{
    try
    {
        using var context = new MessangerBaseContext();

        // Правильный подсчет непрочитанных
        var unreadCount = await context.Messages
            .CountAsync(m => m.SenderId == otherUserId &&
                           m.ReceiverId == userId &&
                           !m.IsRead &&
                           !m.IsDeletedForEveryone &&
                           !(m.IsDeletedForReceiver && m.ReceiverId == userId));

        // Находим или создаем ChatPreview
        var chatPreview = await context.ChatPreviews
            .FirstOrDefaultAsync(c => c.UserId == userId &&
                                    c.OtherUserId == otherUserId);

        if (chatPreview == null)
        {
            // Если чата нет - создаем (это backup, основной создается в CreateChatPreviewAsync)
            var otherUser = await context.Users.FindAsync(otherUserId);
            if (otherUser != null)
            {
                chatPreview = new ChatPreview
                {
                    UserId = userId,
                    OtherUserId = otherUserId,
                    OtherUserName = otherUser.UserName,
                    LastMessage = "Чат начат",
                    LastMessageTime = DateTime.Now,
                    UnreadCount = unreadCount
                };
                context.ChatPreviews.Add(chatPreview);
            }
        }
        else
        {
            // Обновляем существующий
            chatPreview.UnreadCount = unreadCount;
        }

        await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"UpdateUnreadCountAsync error: {ex.Message}");
    }
}


		private List<double> GenerateWaveformData(string audioPath)
		{
			try
			{
				var result = new List<double>();
				using (var audioFile = new AudioFileReader(audioPath))
				{
					// Анализируем первые 5 секунд записи
					var sampleRate = audioFile.WaveFormat.SampleRate;
					var channels = audioFile.WaveFormat.Channels;
					var samplesToAnalyze = sampleRate * channels * 5; // 5 секунд
					var buffer = new float[samplesToAnalyze];
					var samplesRead = audioFile.Read(buffer, 0, samplesToAnalyze);

					if (samplesRead == 0)
						return GenerateRandomWaveform();

					// Разбиваем на 30 сегментов
					int segmentSize = samplesRead / 30;
					for (int i = 0; i < 30; i++)
					{
						var segment = buffer.Skip(i * segmentSize).Take(segmentSize);
						var max = segment.Max();
						var min = segment.Min();
						var range = max - min;
						if (range == 0) range = 1;

						result.Add(5 + (max / range * 20));
					}
				}
				return result;
			}
			catch
			{
				return GenerateRandomWaveform();
			}
		}

		private void PlayNextVoiceMessage(Message currentMessage)
		{
			// Найти индекс текущего сообщения в коллекции
			int currentIndex = Messages.IndexOf(currentMessage);
			if (currentIndex == -1) return;

			// Ищем следующее голосовое сообщение
			for (int i = currentIndex + 1; i < Messages.Count; i++)
			{
				if (Messages[i].IsVoiceMessage)
				{
					PlayVoiceMessage(Messages[i]);
					return;
				}
			}

			// Если не нашли следующее голосовое, можно проиграть первое в списке
			// Или просто ничего не делать, в зависимости от желаемого поведения
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

		private void GoBack()
		{
			// Очищаем шапку перед выходом
			Application.Current.Dispatcher.Invoke(() =>
			{
				ChatHeader.OtherUser = null;
				ChatHeader.AvatarImage = null;
				ChatHeader.IsOnline = false;
				ChatHeader.LastSeen = null;
				ChatHeader.TypingStatus = string.Empty;
			});

			// Навигация назад к списку чатов
			var navigationVM = App.ServiceProvider.GetService<NavigationViewModel>();
			navigationVM?.ShowChatsCommand.Execute(null);
		}

		private void StartVoiceCall()
		{
			MessageBox.Show("Голосовой вызов будет реализован позже");
		}

		private void StartVideoCall()
		{
			MessageBox.Show("Видеозвонок будет реализован позже");
		}

		private void ShowChatMenu()
		{
			// Показываем контекстное меню с дополнительными опциями
			var contextMenu = new ContextMenu
			{
				Items =
		{
			new MenuItem { Header = "Информация о чате", Command = new RelayCommand(ShowChatInfo) },
			new MenuItem { Header = "Очистить историю", Command = new RelayCommand(ClearChatHistory) },
			new MenuItem { Header = "Удалить чат", Command = new RelayCommand(DeleteChat) },
			new Separator(),
			new MenuItem { Header = "Заблокировать пользователя", Command = new RelayCommand(BlockUser) }
		}
			};

			contextMenu.IsOpen = true;
		}

		private void ShowChatInfo()
		{
			MessageBox.Show("Информация о чате будет реализована позже");
		}

		private async void ClearChatHistory()
		{
			await DeleteAllMessages();
		}

		private async void DeleteChat()
		{
			var result = MessageBox.Show("Вы уверены, что хотите удалить этот чат?",
				"Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

			if (result == MessageBoxResult.Yes)
			{
				await DeleteAllMessages();
				GoBack();
			}
		}

		private void BlockUser()
		{
			MessageBox.Show("Блокировка пользователя будет реализована позже");
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string propertyname = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));

	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
namespace MusicMessage.Models;

public partial class Message : INotifyPropertyChanged
{
	public int MessageId { get; set; }

	public string? ContentMess { get; set; }

	public int SenderId { get; set; }

	public int ReceiverId { get; set; }

	public int CurrentUserId { get; set; }

	public DateTime Timestamp { get; set; }

	public string MessageType { get; set; } = null!;

	private bool _isPlaying;
	private bool _isPaused;
	public int? ReplyToMessageId { get; set; }

	[ForeignKey("ReplyToMessageId")]
	public virtual Message ReplyToMessage { get; set; }

	[NotMapped]
	public bool HasReply => ReplyToMessageId.HasValue;

	[NotMapped]
	public bool IsPlaying
	{
		get => _isPlaying;
		set
		{
			if (_isPlaying != value)
			{
				_isPlaying = value;
				OnPropertyChanged();
			}
		}
	}

	[NotMapped]
	public bool IsPaused
	{
		get => _isPaused;
		set
		{
			if (_isPaused != value)
			{
				_isPaused = value;
				OnPropertyChanged();
			}
		}
	}

	[NotMapped]
	public double PlaybackPosition { get; set; }

	public string? AudioPath { get; set; }

	public TimeSpan? Duration { get; set; }

	public string? StickerId { get; set; }

	[NotMapped]
	public bool IsVoiceMessage => !string.IsNullOrEmpty(AudioPath) || MessageType == "Voice";
	[NotMapped]
	public string? WaveformData { get; set; } // Хранится в БД как строка

	[NotMapped]
	public List<double> WaveformDataList
	{
		get => string.IsNullOrEmpty(WaveformData)
			 ? new List<double>()
			 : WaveformData.Split(',', StringSplitOptions.RemoveEmptyEntries)
				   .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
				   .ToList();
		set => WaveformData = value != null && value.Count > 0
			 ? string.Join(",", value.Select(d => d.ToString(CultureInfo.InvariantCulture)))
			 : null;
	}
	public Message()
	{
		MessageType = "Text"; // Значение по умолчанию
		Reactions = new ObservableCollection<Reaction>();
	}

	[NotMapped]
	public MediaPlayer Player { get; private set; }

	[NotMapped]
	public DispatcherTimer PlaybackTimer { get; private set; }
	public void InitializePlayer()
	{
		Player = new MediaPlayer();
		PlaybackTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(100)
		};
	}
	public bool IsDeletedForSender { get; set; } = false;
	public bool IsDeletedForEveryone { get; set; } = false;
	public bool IsDeletedForReceiver { get; set; } = false;
	[NotMapped]
	public bool IsVisible
	{
		get
		{
			if (IsDeletedForEveryone) return false;
			if (IsDeletedForSender && SenderId == CurrentUserId) return false;
			if (IsDeletedForReceiver && ReceiverId == CurrentUserId) return false;
			return true;
		}
	}

	[NotMapped]
	public bool IsEditable
	{
		get
		{
			return SenderId == CurrentUserId &&
				 (DateTime.Now - Timestamp).TotalHours <= 24 &&
				 !IsVoiceMessage;
		}
	}

	public void CleanupPlayer()
	{
		if (Player != null)
		{
			Player.Close();
			Player = null;
		}
		if (PlaybackTimer != null)
		{
			PlaybackTimer.Stop();
			PlaybackTimer = null;
		}
		IsPlaying = false;
		IsPaused = false;
		PlaybackPosition = 0;
		CurrentPlaybackTime = null; // Сбрасываем текущее время
	}

	private string _currentPlaybackTime;

	[NotMapped]
	public string CurrentPlaybackTime
	{
		get => _currentPlaybackTime;
		set
		{
			_currentPlaybackTime = value;
			OnPropertyChanged();
		}
	}
	private ObservableCollection<Reaction> _reactions;
	[NotMapped]
	public ObservableCollection<Reaction> Reactions
	{
		get => _reactions;
		set
		{
			if (_reactions != value)
			{
				// Отписываемся от старой коллекции
				if (_reactions != null)
				{
					_reactions.CollectionChanged -= Reactions_CollectionChanged;
				}

				_reactions = value;

				// Подписываемся на новую коллекцию
				if (_reactions != null)
				{
					_reactions.CollectionChanged += Reactions_CollectionChanged;
				}

				OnPropertyChanged();
				OnPropertyChanged(nameof(ReactionsSummary));
			}
		}
	}
	public virtual ICollection<Reaction> MessageReactions { get; set; } = new List<Reaction>();
	[NotMapped]
	public string ReactionsSummary
	{
		get
		{
			if (Reactions == null || Reactions.Count == 0)
				return null;

			// Группируем реакции по эмодзи и формируем строку вида "👍 2 ❤️ 1"
			var summary = Reactions
				.GroupBy(r => r.Emoji)
				.OrderByDescending(g => g.Count())
				.ThenBy(g => g.Key)
				.Select(g => $"{g.Key} {g.Count()}");

			return string.Join(" ", summary);
		}
	}
	
	//ДОБАВЬTE метод для обновления реакции от конкретного пользователя
	[NotMapped]
	public ICommand ToggleReactionCommand { get; set; }
	// Эта команда будет назначена из ViewModel
	public bool IsRead { get; set; } = false;
	public bool IsEdited { get; set; } = false;
	public virtual User Receiver { get; set; } = null!;

	public virtual User Sender { get; set; } = null!;

	public event PropertyChangedEventHandler PropertyChanged;
	public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
	private void Reactions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
	{
		OnPropertyChanged(nameof(ReactionsSummary));
		Application.Current.Dispatcher.BeginInvoke(() =>
		{
			OnPropertyChanged(nameof(Reactions));
		}, DispatcherPriority.Render);
	}
}
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MusicMessage.Models;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
namespace MusicMessage.ViewModels
{
	public class ChatHeaderViewModel : INotifyPropertyChanged
	{
		private User _otherUser;
		private bool _isOnline;
		private DateTime? _lastSeen;
		private string _typingStatus;
		private BitmapImage _avatarImage;

		public User OtherUser
		{
			get => _otherUser;
			set
			{
				if (_otherUser != value)
				{
					_otherUser = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(StatusText));
				}
			}
		}

		public BitmapImage AvatarImage
		{
			get => _avatarImage;
			set
			{
				if (_avatarImage != value)
				{
					_avatarImage = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsOnline
		{
			get => _isOnline;
			set
			{
				if (_isOnline != value)
				{
					_isOnline = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(StatusText));
				}
			}
		}

		public DateTime? LastSeen
		{
			get => _lastSeen;
			set
			{
				if (_lastSeen != value)
				{
					_lastSeen = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(StatusText));
				}
			}
		}

		public string TypingStatus
		{
			get => _typingStatus;
			set
			{
				if (_typingStatus != value)
				{
					_typingStatus = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(StatusText));
				}
			}
		}

		public string StatusText
		{
			get
			{
				if (!string.IsNullOrEmpty(TypingStatus))
					return TypingStatus;

				if (IsOnline)
					return "онлайн";

				if (LastSeen.HasValue)
					return $"был(а) в {LastSeen.Value:HH:mm}";

				return "не в сети";
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

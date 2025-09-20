using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MusicMessage.Models;
namespace MusicMessage.Models
{
	public class UserSearchResult : INotifyPropertyChanged
	{
		public int UserId { get; set; }
		public string UserName { get; set; }
		public string Email { get; set; }
		public string AvatarPath { get; set; }
		public bool IsOnline { get; set; }
		public string LastName { get; set; }
		public string FirstName { get; set; }
		public DateTime? LastSeen { get; set; }

		private FriendshipStatus? _friendshipStatus;
		public FriendshipStatus? FriendshipStatus
		{
			get => _friendshipStatus;
			set
			{
				_friendshipStatus = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(FriendshipStatusText));
			}
		}

		public bool IsCurrentUser { get; set; }
		public string FriendshipStatusText => GetFriendshipStatusText();

		private string GetFriendshipStatusText()
		{
			if (!FriendshipStatus.HasValue) return "Не в друзьях";

			return FriendshipStatus.Value switch
			{
				Models.FriendshipStatus.Pending => "Заявка отправлена",
				Models.FriendshipStatus.Accepted => "Друг",
				Models.FriendshipStatus.Rejected => "Заявка отклонена",
				Models.FriendshipStatus.Blocked => "Заблокирован",
				_ => "Не в друзьях"
			};
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

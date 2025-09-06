using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicMessage.Models
{
	public class UserSearchResult
	{
		public int UserId { get; set; }
		public string UserName { get; set; }
		public string Email { get; set; }
		public string AvatarPath { get; set; }
		public bool IsOnline { get; set; }
		public DateTime? LastSeen { get; set; }
		public FriendshipStatus? FriendshipStatus { get; set; }
		public bool IsCurrentUser { get; set; }
	}
}

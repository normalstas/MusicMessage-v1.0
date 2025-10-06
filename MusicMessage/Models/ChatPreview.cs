using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MusicMessage.Models
{
	[Table("ChatPreviews")]
	public class ChatPreview : INotifyPropertyChanged
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int ChatPreviewId { get; set; }

		[Required]
		public int UserId { get; set; }

		[Required]
		public int OtherUserId { get; set; }

		[Required]
		[MaxLength(50)]
		public string OtherUserName { get; set; }

		[MaxLength(500)]
		public string LastMessage { get; set; }
		[MaxLength(500)]
		public string FirstName { get; set; }
		[MaxLength(500)]
		public string LastName { get; set; }
		[Required]
		public DateTime LastMessageTime { get; set; }
		[NotMapped]
		public string ActualAvatarPath => OtherUser?.AvatarPath ?? AvatarPath;

		public int UnreadCount { get; set; }
		[MaxLength(500)]
		public string AvatarPath { get; set; }
	
		[ForeignKey("UserId")]
		public virtual User User { get; set; }

		[ForeignKey("OtherUserId")]
		public virtual User OtherUser { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;
		public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

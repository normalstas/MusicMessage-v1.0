using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicMessage.Models
{
	public enum FriendshipStatus
	{
		Pending,
		Accepted,
		Rejected,
		Blocked
	}

	[Table("Friendships")]
	public class Friendship
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int FriendshipId { get; set; }

		[Required]
		public int RequesterId { get; set; }

		[Required]
		public int AddresseeId { get; set; }

		[Required]
		public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }

		[ForeignKey("RequesterId")]
		public virtual User Requester { get; set; }

		[ForeignKey("AddresseeId")]
		public virtual User Addressee { get; set; }
	}
}

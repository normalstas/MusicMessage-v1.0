using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicMessage.Models
{
	[Table("CommentLikes")]
	public class CommentLike
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int LikeId { get; set; }

		[Required]
		public int CommentId { get; set; }

		[Required]
		public int UserId { get; set; }

		[Required]
		public DateTime LikedAt { get; set; } = DateTime.UtcNow;

		
		[ForeignKey("CommentId")]
		public virtual PostComment Comment { get; set; }

		[ForeignKey("UserId")]
		public virtual User User { get; set; }
	}
}

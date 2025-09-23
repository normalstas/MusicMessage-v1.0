using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicMessage.Models
{
	[Table("PostComments")]
	public class PostComment
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int CommentId { get; set; }

		[Required]
		public int PostId { get; set; }

		[Required]
		public int AuthorId { get; set; }

		[Required]
		[MaxLength(500)]
		public string Content { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public DateTime? UpdatedAt { get; set; }

		public int? ParentCommentId { get; set; }

		// Навигационные свойства
		[ForeignKey("PostId")]
		public virtual Post Post { get; set; }

		[ForeignKey("AuthorId")]
		public virtual User Author { get; set; }

		[ForeignKey("ParentCommentId")]
		public virtual PostComment ParentComment { get; set; }

		public virtual ICollection<PostComment> Replies { get; set; } = new List<PostComment>();
	}
}

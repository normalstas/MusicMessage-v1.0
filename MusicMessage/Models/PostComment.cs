using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MusicMessage.Models
{
	[Table("PostComments")]
	public class PostComment : INotifyPropertyChanged
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

		public int LikesCount { get; set; } 
		public DateTime? UpdatedAt { get; set; }
		[NotMapped]
		public bool IsLikedByCurrentUser { get; set; }
		public int? ParentCommentId { get; set; }

		
		[ForeignKey("PostId")]
		public virtual Post Post { get; set; }

		[ForeignKey("AuthorId")]
		public virtual User Author { get; set; }

		[ForeignKey("ParentCommentId")]
		public virtual PostComment ParentComment { get; set; }

		public virtual ICollection<PostComment> Replies { get; set; } = new List<PostComment>();
		public virtual ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();

		public event PropertyChangedEventHandler PropertyChanged;
		public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

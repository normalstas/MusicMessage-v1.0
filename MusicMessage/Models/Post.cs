using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.ComponentModel;

namespace MusicMessage.Models
{
	[Table("Posts")]
	public class Post : INotifyPropertyChanged
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int PostId { get; set; }

		[Required]
		public int AuthorId { get; set; }

		[Required]
		[MaxLength(1000)]
		public string Content { get; set; }

		public string? ImagePath { get; set; }
		public string? VideoPath { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public DateTime? UpdatedAt { get; set; }

		public int LikesCount { get; set; }
		public int CommentsCount { get; set; }
		public int SharesCount { get; set; }

		public bool IsPublic { get; set; } = true;

		
		[ForeignKey("AuthorId")]
		public virtual User Author { get; set; }

		public virtual ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
		public virtual ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
		public virtual ICollection<PostShare> Shares { get; set; } = new List<PostShare>();
		public event PropertyChangedEventHandler PropertyChanged;
		public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

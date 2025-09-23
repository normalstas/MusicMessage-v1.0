using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicMessage.Models
{
	[Table("PostShares")]
	public class PostShare
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int ShareId { get; set; }

		[Required]
		public int PostId { get; set; }

		[Required]
		public int UserId { get; set; }

		[MaxLength(500)]
		public string? SharedComment { get; set; }

		[Required]
		public DateTime SharedAt { get; set; } = DateTime.UtcNow;

		// Навигационные свойства
		[ForeignKey("PostId")]
		public virtual Post Post { get; set; }

		[ForeignKey("UserId")]
		public virtual User User { get; set; }
	}
}

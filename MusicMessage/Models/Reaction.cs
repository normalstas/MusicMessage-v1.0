using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicMessage.Models
{
	public class Reaction
	{
		public int ReactionId { get; set; }
		public int MessageId { get; set; }
		public int UserId { get; set; }
		public string Emoji { get; set; }

		[ForeignKey("MessageId")]
		public virtual Message Message { get; set; }

		[ForeignKey("UserId")]
		public virtual User User { get; set; }
	}
}

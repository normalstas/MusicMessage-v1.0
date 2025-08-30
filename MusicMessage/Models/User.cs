using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MusicMessage.Models;

public partial class User
{
	public int UserId { get; set; }

	[Required]
	[MaxLength(50)]
	public string UserName { get; set; } = null!;

	[Required]
	[MaxLength(100)]
	public string Email { get; set; } = null!;

	[Required]
	public string PasswordHash { get; set; } = null!;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public DateTime? LastLogin { get; set; }

	public virtual ICollection<Message> MessageReceivers { get; set; } = new List<Message>();

	public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();
}

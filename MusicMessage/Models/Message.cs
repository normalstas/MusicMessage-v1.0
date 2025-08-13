using System;
using System.Collections.Generic;

namespace MusicMessage.Models;

public partial class Message
{
    public int MessageId { get; set; }

    public string? ContentMess { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public int CurrentUserId { get; set; }

    public DateTime Timestamp { get; set; }

    public string MessageType { get; set; } = null!;

    public string? AudioPath { get; set; }

    public TimeSpan? Duration { get; set; }

    public string? StickerId { get; set; }

    public string? Reactions { get; set; }

    public virtual User Receiver { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}

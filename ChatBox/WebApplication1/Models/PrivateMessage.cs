using System;

namespace ChatAppBackend.Models
{
    public class PrivateMessage
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;

        public int ReceiverId { get; set; }
        public User Receiver { get; set; } = null!;
        public string SenderUsername { get; set; } = null!;
        public string ReceiverUsername { get; set; } = null!;

        public string Text { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; }
        public MessageStatus Status { get; set; } = MessageStatus.Sent;
    }
}

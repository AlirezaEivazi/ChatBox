using ChatAppBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatAppBackend.Data
{
    public class ChatAppDbContext : DbContext
    {
        public ChatAppDbContext(DbContextOptions<ChatAppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<PrivateMessage> PrivateMessages => Set<PrivateMessage>();
        public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
        public DbSet<ChatRoomMember> ChatRoomMembers => Set<ChatRoomMember>();
        public DbSet<MessageEditLog> MessageEditLogs => Set<MessageEditLog>();
        public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();
        public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PrivateMessage>()
                .HasOne(pm => pm.Sender)
                .WithMany(u => u.PrivateMessagesSent)
                .HasForeignKey(pm => pm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PrivateMessage>()
                .HasOne(pm => pm.Receiver)
                .WithMany(u => u.PrivateMessagesReceived)
                .HasForeignKey(pm => pm.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatRoomMember>()
                .HasIndex(c => new { c.ChatRoomId, c.Username })
                .IsUnique();
        }

    }

}

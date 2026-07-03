using Microsoft.EntityFrameworkCore;
using ChatServer.Models;

namespace ChatServer.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) {}

    public DbSet<Room> Rooms => Set<Room>();    
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>()
                .HasIndex(r => r.Name)
                .IsUnique();
        modelBuilder.Entity<Message>()
                .HasOne(m => m.Room)
                .WithMany(r => r.Messages)
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
    }
}
namespace ChatServer.Models;

public class Message
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public Room? Room { get; set; }
}
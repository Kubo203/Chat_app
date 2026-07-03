namespace ChatServer.Dtos;

public record MessageDto(int Id, int RoomId, string Username, string Content, DateTime SentAt);
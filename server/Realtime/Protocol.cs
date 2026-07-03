namespace ChatServer.Realtime;

public record IncomingChat(string? Type, string? Content);

public record OutgoingChat(string Type, string User, string Content, DateTime SentAt);
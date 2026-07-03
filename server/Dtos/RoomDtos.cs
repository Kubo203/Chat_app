namespace ChatServer.Dtos;

public record RoomDto(int Id, string Name, DateTime CreatedAt);

public record CreateRoomRequest(string Name);
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatServer.Data;
using ChatServer.Dtos;
using ChatServer.Models;

namespace ChatServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly ChatDbContext _db;

    public RoomsController(ChatDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetRooms()
    {
        var rooms = await _db.Rooms
                        .OrderBy(r => r.Name)
                        .Select(r => new RoomDto(r.Id, r.Name, r.CreatedAt))
                        .ToListAsync();
        return Ok(rooms);
    }

    [HttpPost]
    public async Task<ActionResult<RoomDto>> CreateRoom(CreateRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Room name is required.");

        var name = request.Name.Trim();

        var exists = await _db.Rooms.AnyAsync(r => r.Name == name);
        if (exists)
            return Conflict($"A room named '{name}' already exists.");

        var room = new Room {Name = name};
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        var dto = new RoomDto(room.Id, room.Name, room.CreatedAt);
        return CreatedAtAction(nameof(GetRooms), null, dto);


    }

    [HttpGet("{roomId:int}/messages")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(int roomId, int limit=50)
    {
        var roomExists = await _db.Rooms.AnyAsync(r => r.Id == roomId);
        if (!roomExists)
            return NotFound($"Room {roomId} not found.");   // 404

        if (limit < 1) limit = 1;
        if (limit > 200) limit = 200;

        var messages = await _db.Messages
                        .Where(m => m.RoomId == roomId)
                        .OrderByDescending(m => m.SentAt)
                        .Take(limit)
                        .OrderBy(m => m.SentAt)
                        .Select(m => new MessageDto(m.Id, m.RoomId, m.Username, m.Content, m.SentAt))
                        .ToListAsync();



        return Ok(messages);
    }

}
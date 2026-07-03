using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace ChatServer.Realtime;

public class ChatConnection
{
    public required WebSocket Socket {get; init;}
    public required string Username {get; init;}

    public SemaphoreSlim SendLock {get;} = new(1,1);
}

public class RoomRegistry
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, ChatConnection>> _rooms = new();
    public void Add(int roomId, Guid id, ChatConnection conn)
    {
        var room = _rooms.GetOrAdd(roomId, _ => new ConcurrentDictionary<Guid, ChatConnection>());
        room[id] = conn;
    }

    public void Remove(int roomId, Guid id)
    {
        if(_rooms.TryGetValue(roomId, out var room))
        {
            room.TryRemove(id, out _);
            if (room.IsEmpty)
                _rooms.TryRemove(roomId, out _);
        }
    }

    public async Task BroadcastAsync(int roomId, string message)
    {
        if(!_rooms.TryGetValue(roomId, out var room))
            return;
        
        var bytes = Encoding.UTF8.GetBytes(message);

        foreach (var conn in room.Values)
        {
            if (conn.Socket.State != WebSocketState.Open)
                continue;

            await conn.SendLock.WaitAsync();

            try
            {
                await conn.Socket.SendAsync(new ArraySegment<byte>(bytes), 
                                            WebSocketMessageType.Text, 
                                            endOfMessage: true, 
                                            CancellationToken.None);
            }
            catch{

            }
            finally{
                conn.SendLock.Release();
            }
        }


    }


}
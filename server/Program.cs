using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ChatServer.Data;
using ChatServer.Models;
using ChatServer.Realtime;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<RoomRegistry>();

var connectionString = 
builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0)))
);


var app = builder.Build();

app.UseWebSockets();

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

app.Map("/ws", async (HttpContext context) => 
{
    if(!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    // --- parse ?room= and ?user= ---
    if (!int.TryParse(context.Request.Query["room"], out var roomId) ||
        string.IsNullOrWhiteSpace(context.Request.Query["user"]))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }
    var username = context.Request.Query["user"].ToString().Trim();

    var registry     = context.RequestServices.GetRequiredService<RoomRegistry>();
    var scopeFactory = context.RequestServices.GetRequiredService<IServiceScopeFactory>();

    // --- make sure the room exists (fresh DbContext scope) ---
    using (var scope = scopeFactory.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        if (!await db.Rooms.AnyAsync(r => r.Id == roomId))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
    }

    // Complete the handshake   
    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var connId = Guid.NewGuid();
    registry.Add(roomId, connId, new ChatConnection { Socket = socket, Username = username });    
    
    var buffer = new byte[4 * 1024];
    try
    {
        while(socket.State == WebSocketState.Open)
        {

            var result = await socket. ReceiveAsync(new ArraySegment<byte>(buffer), 
                                                    CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                                        "Bye", 
                                        CancellationToken.None);
                break;
            }
            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

            IncomingChat? incoming;
            try{incoming = JsonSerializer.Deserialize<IncomingChat>(json, jsonOptions);
            }catch{ continue; }

            if (incoming?.Type != "chat" || string.IsNullOrWhiteSpace(incoming.Content))
                continue;

            var content = incoming.Content.Trim();
            var sentAt = DateTime.UtcNow;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
                db.Messages.Add(new Message{ RoomId = roomId, Username = username, Content = content, SentAt = sentAt});

                await db.SaveChangesAsync();
            }

            var outgoing = JsonSerializer.Serialize(new OutgoingChat("chat", username, content, sentAt), jsonOptions);
            await registry.BroadcastAsync(roomId, outgoing);
        }
    }catch(WebSocketException){

    }finally{
        registry.Remove(roomId,connId);
    }
});

app.MapControllers();

app.Run();

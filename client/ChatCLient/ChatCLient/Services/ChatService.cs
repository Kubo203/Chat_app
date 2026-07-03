using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatCLient.Models;

namespace ChatCLient.Services;

public class ChatService
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private string _me = "";

    // Raised on a BACKGROUND thread when a chat message arrives.
    public event Action<ChatMessage>? MessageReceived;
    // Raised when the connection ends (closed or dropped).
    public event Action<string>? Disconnected;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task ConnectAsync(string baseUrl, int roomId, string user)
    {
        _me = user;

        // http://host:5173  ->  ws://host:5173   (https -> wss)
        var wsBase = baseUrl.TrimEnd('/')
                            .Replace("https://", "wss://")
                            .Replace("http://", "ws://");
        var url = $"{wsBase}/ws?room={roomId}&user={Uri.EscapeDataString(user)}";

        _socket = new ClientWebSocket();
        _cts = new CancellationTokenSource();
        await _socket.ConnectAsync(new Uri(url), CancellationToken.None);

        _ = Task.Run(ReceiveLoopAsync);   // start reading in the background
    }

    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[4 * 1024];
        try
        {
            while (_socket!.State == WebSocketState.Open)
            {
                var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts!.Token);
                if (result.MessageType == WebSocketMessageType.Close) break;

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var msg = JsonSerializer.Deserialize<OutgoingChat>(json, Json);
                if (msg is null || msg.Type != "chat") continue;

                MessageReceived?.Invoke(new ChatMessage
                {
                    User = msg.User,
                    Content = msg.Content,
                    SentAt = msg.SentAt,
                    Mine = string.Equals(msg.User, _me, StringComparison.OrdinalIgnoreCase)
                });
            }
        }
        catch { /* socket dropped or cancelled */ }
        finally { Disconnected?.Invoke("Disconnected."); }
    }

    public async Task SendAsync(string content)
    {
        if (_socket is not { State: WebSocketState.Open }) return;

        var json = JsonSerializer.Serialize(new { type = "chat", content }, Json);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(new ArraySegment<byte>(bytes),
                                WebSocketMessageType.Text, endOfMessage: true,
                                CancellationToken.None);
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _cts?.Cancel();
            if (_socket is { State: WebSocketState.Open })
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye",
                                         CancellationToken.None);
        }
        catch { /* ignore on the way out */ }
        finally { _socket?.Dispose(); }
    }

    // The shape of one live message from the server (note: "User", not "Username").
    private record OutgoingChat(string Type, string User, string Content, DateTime SentAt);
}
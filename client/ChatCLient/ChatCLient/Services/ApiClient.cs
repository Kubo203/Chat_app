using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ChatCLient.Models;
using System.Linq;

namespace ChatCLient.Services;

public class ApiClient
{
    private readonly HttpClient _http = new();

    public async Task<List<RoomModel>> GetRoomsAsync(string baseUrl)
    {
        var url = $"{baseUrl.TrimEnd('/')}/api/rooms";
        var rooms = await _http.GetFromJsonAsync<List<RoomModel>>(url);
        return rooms ?? new List<RoomModel>();
    }

    public async Task<RoomModel> CreateRoomAsync(string baseUrl, string name)
    {
        var url = $"{baseUrl.TrimEnd('/')}/api/rooms";
        var response = await _http.PostAsJsonAsync(url, new {name});
        response.EnsureSuccessStatusCode();
        var room = await response.Content.ReadFromJsonAsync<RoomModel>();
        return room!;
    }

    public async Task<List<ChatMessage>> GetChatMessagesAsync(string baseUrl, int roomId, string me)
    {
        var url = $"{baseUrl.TrimEnd('/')}/api/rooms/{roomId}/messages";
        var rows = await _http.GetFromJsonAsync<List<MessageDto>>(url) ?? new();
        return rows.Select(m => new ChatMessage
        {
            User = m.Username,
            Content = m.Content,
            SentAt = m.SentAt,
            Mine = string.Equals(m.Username, me, StringComparison.OrdinalIgnoreCase)
        })
        .ToList();
    }

    private record MessageDto(int Id, int RoomId, string Username, string Content, DateTime SentAt);
}
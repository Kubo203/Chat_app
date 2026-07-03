using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;                       // Application.Current.Dispatcher
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatCLient.Models;
using ChatCLient.Services;

namespace ChatCLient.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly ApiClient _api = new();
    private readonly ChatService _chat = new();

    private readonly string _baseUrl;
    private readonly int _roomId;
    private readonly string _me;

    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _draft = "";
    [ObservableProperty] private string _status = "";

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public ChatViewModel(string baseUrl, RoomModel room, string username)
    {
        _baseUrl = baseUrl;
        _roomId  = room.Id;
        _me      = username;
        Title    = $"{room.Name} — {username}";

        _chat.MessageReceived += msg    => OnUi(() => Messages.Add(msg));
        _chat.Disconnected    += reason => OnUi(() => Status = reason);
    }

    // Called once the window is shown: history first, then go live.
    public async Task StartAsync()
    {
        try
        {
            Status = "Loading history...";
            var history = await _api.GetMessagesAsync(_baseUrl, _roomId, _me);
            Messages.Clear();
            foreach (var m in history) Messages.Add(m);

            Status = "Connecting...";
            await _chat.ConnectAsync(_baseUrl, _roomId, _me);
            Status = "Connected.";
        }
        catch (Exception ex) { Status = "Error: " + ex.Message; }
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        var text = Draft.Trim();
        if (text.Length == 0) return;
        Draft = "";                                  // clear the box immediately
        try { await _chat.SendAsync(text); }         // server echoes it back to us
        catch (Exception ex) { Status = "Send failed: " + ex.Message; }
    }

    // Called when the window closes.
    public Task StopAsync() => _chat.DisconnectAsync();

    // Hop back onto the UI thread — events fire on a background thread.
    private static void OnUi(Action action) =>
        Application.Current.Dispatcher.Invoke(action);
}
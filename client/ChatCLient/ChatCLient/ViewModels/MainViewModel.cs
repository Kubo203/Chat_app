using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatCLient.Models;
using ChatCLient.Services;

namespace ChatCLient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiClient _api = new();

    [ObservableProperty] private string _serverUrl = "http://192.168.0.25:5173";
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _newRoomName = "";
    [ObservableProperty] private string _status = "";

    // A binding-friendly list: UI updates automatically when items change
    public ObservableCollection<RoomModel> Rooms { get; } = new();

    [RelayCommand]
    private async Task LoadRoomsAsync()
    {
        try
        {
            Status = "Loading rooms...";
            var rooms = await _api.GetRoomsAsync(ServerUrl);
            Rooms.Clear();
            foreach (var r in rooms) Rooms.Add(r);
            Status = $"Loaded {Rooms.Count} room(s).";
        }
        catch (Exception ex) { Status = "Error: " + ex.Message; }
    }

    [RelayCommand]
    private async Task CreateRoomAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRoomName)) { Status = "Enter a room name."; return; }
        try
        {
            var room = await _api.CreateRoomAsync(ServerUrl, NewRoomName.Trim());
            Rooms.Add(room);
            NewRoomName = "";
            Status = $"Created '{room.Name}'.";
        }
        catch (Exception ex) { Status = "Error: " + ex.Message; }
    }
}
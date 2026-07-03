using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChatCLient.Models;
using ChatCLient.ViewModels;

namespace ChatCLient;

public partial class ChatWindow : Window
{
    private readonly ChatViewModel _vm;

    public ChatWindow(string baseUrl, RoomModel room, string username)
    {
        InitializeComponent();
        _vm = new ChatViewModel(baseUrl, room, username);
        DataContext = _vm;

        Loaded += async (_, _) => await _vm.StartAsync();
        Closed += async (_, _) => await _vm.StopAsync();
    }

    private void Draft_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _vm.SendCommand.CanExecute(null))
            _vm.SendCommand.Execute(null);
    }

    private void Rooms_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm && ((ListBox)sender).SelectedItem is RoomModel room)
            vm.OpenRoomCommand.Execute(room);
    }
}
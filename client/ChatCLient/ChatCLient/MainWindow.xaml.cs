using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChatCLient.Models;
using ChatCLient.ViewModels;

namespace ChatCLient;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    // Double-click a room -> open its chat window (uses the username you typed).
    private void Rooms_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm && ((ListBox)sender).SelectedItem is RoomModel room)
            vm.OpenRoomCommand.Execute(room);
    }
}

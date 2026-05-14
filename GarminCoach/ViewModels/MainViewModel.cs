using CommunityToolkit.Mvvm.ComponentModel;
using GarminCoach.Models;

namespace GarminCoach.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    public DashboardViewModel Dashboard { get; }
    public ChatViewModel Chat { get; }
    public SettingsViewModel Settings { get; }

    public MainViewModel(DashboardViewModel dashboard, ChatViewModel chat, SettingsViewModel settings)
    {
        Dashboard = dashboard;
        Chat = chat;
        Settings = settings;
        Dashboard.SnapshotChanged += (_, snap) => Chat.SetSnapshot(snap);
    }
}

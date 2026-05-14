using System.Windows;
using GarminCoach.Services;
using GarminCoach.ViewModels;
using GarminCoach.Views;

namespace GarminCoach;

public partial class MainWindow : Window
{
    private readonly SettingsStore _settings;
    private readonly SettingsViewModel _settingsVm;

    public MainWindow(SettingsStore settings, SettingsViewModel settingsVm)
    {
        InitializeComponent();
        _settings = settings;
        _settingsVm = settingsVm;
        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var s = _settings.Load();
        if (!_settings.IsConfigured(s))
        {
            OpenSettings();
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e) => OpenSettings();

    private void OpenSettings()
    {
        _settingsVm.Reload();
        var dlg = new SettingsDialog(_settingsVm) { Owner = this };
        dlg.ShowDialog();
    }

    private void OnMinimize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (MaximizeBtn != null)
        {
            // E922 = single square (Maximize), E923 = restore (two overlapping squares)
            MaximizeBtn.Content = WindowState == WindowState.Maximized ? "" : "";
        }
    }
}

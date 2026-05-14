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
}

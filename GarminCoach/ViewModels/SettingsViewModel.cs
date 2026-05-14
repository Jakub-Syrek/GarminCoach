using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GarminCoach.Models;
using GarminCoach.Services;

namespace GarminCoach.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore _store;

    [ObservableProperty] private string _garminEmail = string.Empty;
    [ObservableProperty] private string _garminPassword = string.Empty;
    [ObservableProperty] private string _anthropicApiKey = string.Empty;
    [ObservableProperty] private string _model = "claude-sonnet-4-6";
    [ObservableProperty] private int _daysToFetch = 7;
    [ObservableProperty] private bool _saved;

    public SettingsViewModel(SettingsStore store)
    {
        _store = store;
        Reload();
    }

    public void Reload()
    {
        var s = _store.Load();
        GarminEmail = s.GarminEmail;
        GarminPassword = s.GarminPassword;
        AnthropicApiKey = s.AnthropicApiKey;
        Model = string.IsNullOrWhiteSpace(s.Model) ? "claude-sonnet-4-6" : s.Model;
        DaysToFetch = s.DaysToFetch <= 0 ? 7 : s.DaysToFetch;
        Saved = false;
    }

    [RelayCommand]
    private void Save()
    {
        var s = new AppSecrets
        {
            GarminEmail = GarminEmail.Trim(),
            GarminPassword = GarminPassword,
            AnthropicApiKey = AnthropicApiKey.Trim(),
            Model = Model.Trim(),
            DaysToFetch = DaysToFetch
        };
        _store.Save(s);
        Saved = true;
    }
}

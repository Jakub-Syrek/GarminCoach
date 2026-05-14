using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GarminCoach.Models;
using GarminCoach.Services;
using Microsoft.Extensions.Logging;

namespace GarminCoach.ViewModels;

public sealed partial class ChatViewModel : ObservableObject
{
    private readonly IClaudeService _claude;
    private readonly ILogger<ChatViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "Czat z coachem. Najpierw odśwież dane na dashboardzie.";
    private CoachSnapshot _snapshot = CoachSnapshot.Empty();

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public ChatViewModel(IClaudeService claude, ILogger<ChatViewModel> logger)
    {
        _claude = claude;
        _logger = logger;
    }

    public void SetSnapshot(CoachSnapshot snapshot)
    {
        _snapshot = snapshot;
        StatusText = snapshot.IsEmpty
            ? "Czat z coachem. Najpierw odśwież dane na dashboardzie."
            : $"Coach ma dostęp do {snapshot.Days.Count} dni danych i {snapshot.RecentActivities.Count} aktywności.";
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || IsBusy) return;

        var prompt = InputText.Trim();
        InputText = string.Empty;

        Messages.Add(new ChatMessage { Role = ChatRole.User, Content = prompt });

        try
        {
            IsBusy = true;
            var history = Messages.Take(Messages.Count - 1).ToList();
            var reply = await _claude.AskAsync(_snapshot, history, prompt);
            Messages.Add(new ChatMessage { Role = ChatRole.Assistant, Content = reply });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat send failed");
            Messages.Add(new ChatMessage
            {
                Role = ChatRole.Assistant,
                Content = "Błąd: " + ex.Message
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSend() => !string.IsNullOrWhiteSpace(InputText) && !IsBusy;

    partial void OnInputTextChanged(string value) => SendCommand.NotifyCanExecuteChanged();
    partial void OnIsBusyChanged(bool value) => SendCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void Clear() => Messages.Clear();
}

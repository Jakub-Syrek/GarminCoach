namespace GarminCoach.Models;

public enum ChatRole { User, Assistant, System }

public sealed class ChatMessage
{
    public ChatRole Role { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.Now;

    public bool IsUser => Role == ChatRole.User;
    public bool IsAssistant => Role == ChatRole.Assistant;
}

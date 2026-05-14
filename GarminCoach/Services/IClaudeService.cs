using GarminCoach.Models;

namespace GarminCoach.Services;

public interface IClaudeService
{
    Task<string> AskAsync(
        CoachSnapshot snapshot,
        IReadOnlyList<ChatMessage> conversation,
        string userPrompt,
        CancellationToken ct = default);
}

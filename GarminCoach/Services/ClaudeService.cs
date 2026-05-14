using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using GarminCoach.Models;
using Microsoft.Extensions.Logging;

namespace GarminCoach.Services;

public sealed class ClaudeService : IClaudeService
{
    private readonly SettingsStore _settings;
    private readonly ILogger<ClaudeService> _logger;

    public ClaudeService(SettingsStore settings, ILogger<ClaudeService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> AskAsync(
        CoachSnapshot snapshot,
        IReadOnlyList<ChatMessage> conversation,
        string userPrompt,
        CancellationToken ct = default)
    {
        var secrets = _settings.Load();
        if (string.IsNullOrWhiteSpace(secrets.AnthropicApiKey))
        {
            throw new InvalidOperationException("Anthropic API key not configured.");
        }

        using var client = new AnthropicClient(new APIAuthentication(secrets.AnthropicApiKey));

        var system = new List<SystemMessage>
        {
            new(CoachContextBuilder.SystemPrompt(snapshot), null!)
        };

        var messages = new List<Message>();
        foreach (var m in conversation)
        {
            if (m.Role == ChatRole.System) continue;
            var role = m.Role == ChatRole.User ? RoleType.User : RoleType.Assistant;
            messages.Add(new Message(role, m.Content, null!));
        }
        messages.Add(new Message(RoleType.User, userPrompt, null!));

        var parameters = new MessageParameters
        {
            Model = string.IsNullOrWhiteSpace(secrets.Model) ? "claude-sonnet-4-6" : secrets.Model,
            MaxTokens = 1024,
            Temperature = 0.7m,
            System = system,
            Messages = messages
        };

        var response = await client.Messages.GetClaudeMessageAsync(parameters, ct);
        if (response?.Content is { Count: > 0 } content)
        {
            var texts = content
                .OfType<TextContent>()
                .Select(t => t.Text)
                .Where(t => !string.IsNullOrEmpty(t));
            var combined = string.Concat(texts);
            if (combined.Length > 0) return combined;
        }
        if (response?.FirstMessage?.Text is { Length: > 0 } first)
        {
            return first;
        }
        return "(Claude nie zwrócił odpowiedzi.)";
    }
}

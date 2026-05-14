namespace GarminCoach.Models;

public sealed class AppSecrets
{
    public string GarminEmail { get; set; } = string.Empty;
    public string GarminPassword { get; set; } = string.Empty;
    public string AnthropicApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-6";
    public int DaysToFetch { get; set; } = 7;
}

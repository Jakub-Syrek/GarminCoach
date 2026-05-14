using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GarminCoach.Models;

namespace GarminCoach.Services;

public sealed class SettingsStore
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GarminCoach");
    private static readonly string SecretsPath = Path.Combine(Dir, "secrets.bin");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = false
    };

    public AppSecrets Load()
    {
        if (!File.Exists(SecretsPath))
        {
            return new AppSecrets();
        }

        try
        {
            var protectedBytes = File.ReadAllBytes(SecretsPath);
            var plain = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(plain);
            return JsonSerializer.Deserialize<AppSecrets>(json, Json) ?? new AppSecrets();
        }
        catch
        {
            return new AppSecrets();
        }
    }

    public void Save(AppSecrets secrets)
    {
        Directory.CreateDirectory(Dir);
        var json = JsonSerializer.Serialize(secrets, Json);
        var plain = Encoding.UTF8.GetBytes(json);
        var protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(SecretsPath, protectedBytes);
    }

    public bool IsConfigured(AppSecrets s) =>
        !string.IsNullOrWhiteSpace(s.GarminEmail) &&
        !string.IsNullOrWhiteSpace(s.GarminPassword) &&
        !string.IsNullOrWhiteSpace(s.AnthropicApiKey);
}

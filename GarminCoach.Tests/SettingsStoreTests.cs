using GarminCoach.Models;
using GarminCoach.Services;

namespace GarminCoach.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void IsConfigured_RequiresAllThreeFields()
    {
        var store = new SettingsStore();

        Assert.False(store.IsConfigured(new AppSecrets()));
        Assert.False(store.IsConfigured(new AppSecrets { GarminEmail = "a@b" }));
        Assert.False(store.IsConfigured(new AppSecrets
        {
            GarminEmail = "a@b",
            GarminPassword = "pw"
        }));
        Assert.True(store.IsConfigured(new AppSecrets
        {
            GarminEmail = "a@b",
            GarminPassword = "pw",
            AnthropicApiKey = "sk-ant-xxx"
        }));
    }

    [Fact]
    public void IsConfigured_TreatsWhitespaceAsMissing()
    {
        var store = new SettingsStore();

        Assert.False(store.IsConfigured(new AppSecrets
        {
            GarminEmail = "   ",
            GarminPassword = "pw",
            AnthropicApiKey = "sk-ant"
        }));
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsAllFields()
    {
        // Note: this writes to the real %AppData%\GarminCoach\secrets.bin.
        // We snapshot the original contents, overwrite, and restore at the end.
        var store = new SettingsStore();
        var original = store.Load();

        try
        {
            var secrets = new AppSecrets
            {
                GarminEmail = "test@example.com",
                GarminPassword = "p@ssw0rd!",
                AnthropicApiKey = "sk-ant-test-key-12345",
                Model = "claude-haiku-4-5-20251001",
                DaysToFetch = 14
            };

            store.Save(secrets);
            var loaded = store.Load();

            Assert.Equal(secrets.GarminEmail, loaded.GarminEmail);
            Assert.Equal(secrets.GarminPassword, loaded.GarminPassword);
            Assert.Equal(secrets.AnthropicApiKey, loaded.AnthropicApiKey);
            Assert.Equal(secrets.Model, loaded.Model);
            Assert.Equal(secrets.DaysToFetch, loaded.DaysToFetch);
        }
        finally
        {
            store.Save(original);
        }
    }
}

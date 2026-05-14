using System.Windows;
using GarminCoach.Services;
using GarminCoach.ViewModels;
using GarminCoach.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GarminCoach;

public partial class App : Application
{
    public IHost? Host { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<SettingsStore>();
                services.AddSingleton<IGarminService, GarminService>();
                services.AddSingleton<IClaudeService, ClaudeService>();

                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<ChatViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<MainViewModel>();

                services.AddSingleton<MainWindow>();
            })
            .ConfigureLogging(b => b.SetMinimumLevel(LogLevel.Information))
            .Build();

        var main = Host.Services.GetRequiredService<MainWindow>();
        main.DataContext = Host.Services.GetRequiredService<MainViewModel>();
        main.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Host?.Dispose();
        base.OnExit(e);
    }

    public T Resolve<T>() where T : notnull
    {
        if (Host == null) throw new InvalidOperationException("Host not started.");
        return Host.Services.GetRequiredService<T>();
    }
}

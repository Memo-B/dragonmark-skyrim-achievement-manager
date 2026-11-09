using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using Dragonmark.Infrastructure;
using Dragonmark.Steam;

namespace Dragonmark.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        base.OnStartup(e);
        int screenshot = Array.IndexOf(e.Args, "--screenshot");
        int suite = Array.IndexOf(e.Args, "--capture-suite");
        int capture = screenshot >= 0 ? screenshot : suite;
        if (capture >= 0 && (!e.Args.Contains("--demo") || e.Args.Contains("--probe-steam") || capture + 1 >= e.Args.Length || !Path.IsPathFullyQualified(e.Args[capture + 1])))
        {
            MessageBox.Show("Screenshot capture requires --demo and an absolute output path.", "Dragonmark", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown(2);
            return;
        }
        // Screenshot fixtures have their own profile, so public screenshots never read
        // a real account snapshot or change the user's ordinary settings/history.
        var data = new LocalData(capture >= 0 ? Path.Combine(Path.GetTempPath(), "Dragonmark-screenshots", Guid.NewGuid().ToString("N")) : null);
        if (e.Args.Contains("--probe-steam"))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var reportPath = Path.Combine(data.Root, "steam-probe.json");
            await using var provider = new SteamAchievementProvider(new SteamApi(data.Log), data.Log);
            object report;
            int exit = 0;
            try
            {
                var items = await provider.ConnectAsync(CancellationToken.None);
                int icons = 0, lockedIcons = 0, unlockedIcons = 0;
                if (e.Args.Contains("--probe-icons"))
                    foreach (var item in items)
                        if (await provider.GetIconAsync(item.ApiId, CancellationToken.None) is not null) { icons++; if (item.Unlocked) unlockedIcons++; else lockedIcons++; }
                report = new { Timestamp = DateTimeOffset.UtcNow, ReadOnly = true, Connected = true, Count = items.Count, ChangesTested = false, IconsRequested = e.Args.Contains("--probe-icons"), IconsLoaded = icons, LockedIcons = lockedIcons, UnlockedIcons = unlockedIcons, provider.Info.Mode };
            }
            catch (Exception error) { exit = 1; report = new { Timestamp = DateTimeOffset.UtcNow, ReadOnly = true, Connected = false, Error = error.Message, ChangesTested = false }; }
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            Shutdown(exit);
            return;
        }
        var viewModel = new MainViewModel(data);
        var window = new MainWindow(viewModel);
        MainWindow = window;
        window.Show();
        if (e.Args.Contains("--demo")) await viewModel.ConnectDemoAsync();
        if (suite >= 0)
        {
            await ScreenshotCapture.SuiteAsync(window, viewModel, e.Args[suite + 1], e.Args.Contains("--qa"));
            window.Close();
        }
        if (screenshot >= 0 && screenshot + 1 < e.Args.Length)
        {
            await Task.Delay(400);
            window.SaveScreenshot(Path.GetFullPath(e.Args[screenshot + 1]));
            window.Close();
        }
    }
}

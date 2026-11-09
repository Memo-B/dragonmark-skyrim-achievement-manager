using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dragonmark.Core;

namespace Dragonmark.App;

internal static class ScreenshotCapture
{
    public static void Save(Window window, string path, double scale = 1)
    {
        window.UpdateLayout();
        var content = (FrameworkElement)window.Content;
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth * scale), (int)Math.Ceiling(content.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = File.Create(path); encoder.Save(stream);
    }
    public static async Task SuiteAsync(MainWindow window, MainViewModel vm, string folder, bool qa = false)
    {
        if (!vm.IsDemo) throw new InvalidOperationException("Public screenshots require Demo Mode.");
        vm.ReduceMotion = true;
        async Task Capture(string file)
        {
            await Task.Delay(180);
            Save(window, Path.Combine(folder, file + ".png"));
        }
        vm.Page = "Dashboard"; vm.Detail = vm.Rows.First();
        await Capture("dashboard");
        vm.Page = "All Achievements"; vm.Detail = vm.Rows.First();
        await Capture("achievements-gallery");
        vm.Detail = vm.Rows.First(r => r.ApiId == "DEMO_IRON_OATH");
        await Capture("achievement-details");
        var confirmation = vm.CreateDemoCaptureConfirmation();
        confirmation.Show(); await Task.Delay(180);
        Save(confirmation, Path.Combine(folder, "unlock-all-confirmation.png"));
        confirmation.Close(); // No confirmation and no provider mutation.
        vm.Filter = AchievementFilter.Rare; vm.Detail = vm.Rows.First();
        await Capture("achievement-filters");
        vm.Page = "Settings"; await Capture("settings");
        vm.Page = "Diagnostics"; await Capture("diagnostics");
        vm.Page = "Dashboard";
        if (qa)
        {
            foreach (string page in new[] { "Presets", "Activity History", "About", "Locked", "Unlocked" })
            {
                vm.Page = page; await Capture(page.ToLowerInvariant().Replace(' ', '-'));
            }
            vm.Page = "All Achievements"; vm.Search = "no-such-achievement"; await Capture("empty-state");
            vm.Search = ""; vm.Page = "Dashboard";
            window.Width = 1100; window.Height = 740; await Capture("compact-1100x740");
            window.Width = 1440; window.Height = 960; await Task.Delay(180);
            Save(window, Path.Combine(folder, "render-scale-150.png"), 1.5);
            foreach (double scale in new[] { 1.25, 1.5, 2.0 })
            {
                var workArea = new Rect(0, 0, 1366 / scale, 728 / scale);
                WindowLayout.Constrain(window, workArea);
                var viewport = (ScrollViewer)window.Content;
                viewport.ScrollToBottom(); viewport.ScrollToRightEnd();
                await Capture($"workarea-{scale * 100:0}-dashboard");
                var compact = vm.CreateDemoCaptureConfirmation();
                compact.Show(); WindowLayout.Constrain(compact, workArea, 520, 360);
                await Task.Delay(180);
                var dialogViewport = (ScrollViewer)compact.Content;
                dialogViewport.ScrollToBottom(); dialogViewport.ScrollToRightEnd();
                await Task.Delay(180);
                Save(compact, Path.Combine(folder, $"workarea-{scale * 100:0}-confirmation.png"));
                compact.Close();
            }
            var dpi = VisualTreeHelper.GetDpi(window);
            File.WriteAllText(Path.Combine(folder, "capture-environment.json"), System.Text.Json.JsonSerializer.Serialize(new { dpi.PixelsPerInchX, dpi.PixelsPerInchY, RasterScaleTest = 1.5, Mode = "Demo", WritesExecuted = false }));
        }
    }
}

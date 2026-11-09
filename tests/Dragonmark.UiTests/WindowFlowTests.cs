using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Dragonmark.App;
using Dragonmark.Core;
using Dragonmark.Infrastructure;
using Xunit;

namespace Dragonmark.UiTests;

[Collection("WPF")]
public sealed class WindowFlowTests(WpfHost host)
{
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T target) yield return target;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static void Click(Button button) => ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
    private static MainViewModel DemoModel() => new(new LocalData(Path.Combine(Path.GetTempPath(), "Dragonmark-ui-tests", Guid.NewGuid().ToString("N"))));
    private static async Task Settle() => await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public Task MainWindowFitsLaptopWorkAreaAndExposesBottomControls(double scale) => host.Run(async () =>
    {
        await using var vm = DemoModel();
        var window = new MainWindow(vm); Application.Current.MainWindow = window;
        window.Show(); await vm.ConnectDemoAsync();
        WindowLayout.Constrain(window, new Rect(0, 0, 1366 / scale, 728 / scale));
        await Settle(); window.UpdateLayout();
        Assert.True(window.ActualWidth <= 1366 / scale);
        Assert.True(window.ActualHeight <= 728 / scale);
        var viewport = Assert.IsType<ScrollViewer>(window.Content);
        viewport.ScrollToBottom(); viewport.ScrollToRightEnd(); await Settle(); window.UpdateLayout();
        var cancel = Descendants<Button>(window).Single(b => Equals(b.Content, "Cancel read"));
        Rect bounds = cancel.TransformToAncestor(viewport).TransformBounds(new Rect(cancel.RenderSize));
        Assert.True(bounds.Bottom <= viewport.ActualHeight + 1, $"Bottom control is outside viewport: {bounds.Bottom} > {viewport.ActualHeight}");
        Assert.True(bounds.Right <= viewport.ActualWidth + 1);
        window.Close(); await Settle();
    });

    [Theory]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public Task ConfirmationFitsScaledWorkAreaAndCancelRemainsReachable(double scale) => host.Run(async () =>
    {
        var dialog = new ConfirmationWindow("Review", "Fictional test only", "FIXTURE", 1);
        dialog.Show();
        WindowLayout.Constrain(dialog, new Rect(0, 0, 1366 / scale, 728 / scale), 520, 360);
        await Settle(); dialog.UpdateLayout();
        var viewport = Assert.IsType<ScrollViewer>(dialog.Content);
        viewport.ScrollToBottom(); viewport.ScrollToRightEnd(); await Settle(); dialog.UpdateLayout();
        var cancel = Descendants<Button>(dialog).Single(b => b.IsCancel);
        var confirm = Descendants<Button>(dialog).Single(b => Equals(b.Content, "Confirm 1 changes"));
        foreach (var button in new[] { cancel, confirm })
        {
            var bounds = button.TransformToAncestor(viewport).TransformBounds(new Rect(button.RenderSize));
            Assert.True(bounds.Top >= 0 && bounds.Bottom <= viewport.ActualHeight + 1);
            Assert.True(bounds.Left >= 0 && bounds.Right <= viewport.ActualWidth + 1);
        }
        Assert.True(cancel.IsDefault);
        Assert.False(confirm.IsDefault);
        dialog.Close();
    });

    [Theory]
    [InlineData("Enter")]
    [InlineData("Escape")]
    [InlineData("Confirm")]
    public Task UnlockAllUsesOneModalAndOnlyExplicitConfirmationChangesDemo(string action) => host.Run(async () =>
    {
        await using var vm = DemoModel();
        var owner = new MainWindow(vm); Application.Current.MainWindow = owner;
        owner.Show(); await vm.ConnectDemoAsync(); vm.Search = "summit";
        Assert.Single(vm.Rows);
        Assert.Equal(2, vm.Unlocked);
        Assert.False(vm.UnlockCommand.CanExecute(null));
        int dialogs = 0; Exception? failure = null; bool duplicateBlocked = false;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
        timer.Tick += async (_, _) =>
        {
            var dialog = Application.Current.Windows.OfType<ConfirmationWindow>().FirstOrDefault();
            if (dialog is null) return;
            timer.Stop(); dialogs++;
            try
            {
                Assert.True(vm.Busy);
                Assert.False(vm.UnlockAllCommand.CanExecute(null));
                await vm.UnlockAllAsync(); duplicateBlocked = true;
                var confirm = Descendants<Button>(dialog).Single(b => Equals(b.Content, "Confirm 6 changes"));
                var cancel = Descendants<Button>(dialog).Single(b => b.IsCancel);
                Assert.True(cancel.IsDefault);
                if (action == "Confirm") Click(confirm);
                else { dialog.Activate(); cancel.Focus(); await Settle(); Assert.True(AccessKeyManager.IsKeyRegistered(PresentationSource.FromVisual(dialog), action == "Enter" ? "\r" : "\x1b")); AccessKeyManager.ProcessKey(PresentationSource.FromVisual(dialog), action == "Enter" ? "\r" : "\x1b", false); }
            }
            catch (Exception e) { failure = e; dialog.Close(); }
        };
        timer.Start();
        try { await vm.UnlockAllAsync(); }
        finally { timer.Stop(); }
        if (failure is not null) throw failure;
        Assert.Equal(1, dialogs); Assert.True(duplicateBlocked);
        Assert.Equal(action == "Confirm" ? 8 : 2, vm.Unlocked);
        Assert.False(vm.Busy);
        if (action == "Confirm") Assert.Contains("6 successful · 0 failed · 0 unverified", vm.BatchSummary);
        owner.Close(); await Settle();
    });

    [Fact]
    public Task KeyboardFocusCanMoveBetweenDialogActions() => host.Run(async () =>
    {
        var dialog = new ConfirmationWindow("Review", "Fictional test only", "FIXTURE", 1);
        dialog.Show(); await Settle();
        var cancel = Descendants<Button>(dialog).Single(b => b.IsCancel);
        var confirm = Descendants<Button>(dialog).Single(b => Equals(b.Content, "Confirm 1 changes"));
        cancel.Focus();
        Assert.True(cancel.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
        Assert.Same(confirm, FocusManager.GetFocusedElement(dialog));
        dialog.Close();
    });
}

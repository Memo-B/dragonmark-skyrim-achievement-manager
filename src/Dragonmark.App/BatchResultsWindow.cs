using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dragonmark.Core;

namespace Dragonmark.App;

public sealed class BatchResultsWindow : Window
{
    public BatchResultsWindow(BatchResult result)
    {
        SetResourceReference(StyleProperty, typeof(Window));
        Title = "Batch results · Dragonmark"; Width = 860; Height = 740; MinWidth = 550; MinHeight = 440;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new Grid(); root.Children.Add(new NordicAtmosphere { Opacity = .4, IsHitTestVisible = false });
        var panel = new DockPanel { Margin = new Thickness(28) };
        var head = new StackPanel();
        head.Children.Add(new TextBlock { Text = result.IsDemo ? "DEMO MODE · No Steam account changed" : "LIVE STEAM · Persistence evidence", Foreground = (Brush)FindResource("Gold"), FontSize = 11 });
        head.Children.Add(new TextBlock { Text = "The record of your marks", FontFamily = (FontFamily)FindResource("DisplayFont"), FontSize = 29, Margin = new Thickness(0, 12, 0, 20) });
        var counters = new UniformGridCompat();
        counters.Children.Add(Counter(result.VerifiedCount, "SUCCESSFUL · VERIFIED", "#BBD5C6"));
        counters.Children.Add(Counter(result.FailedCount, "FAILED · REJECTED", "#E1AAA5"));
        counters.Children.Add(Counter(result.UnverifiedCount, "UNVERIFIED · UNCERTAIN", "#D5BD86"));
        head.Children.Add(counters);
        head.Children.Add(new TextBlock { Text = "Local acceptance and submission are intermediate states. A verified result requires store acknowledgement and a matching refreshed state.", Foreground = (Brush)FindResource("Muted"), FontSize = 11, Margin = new Thickness(0, 15, 0, 18) });
        DockPanel.SetDock(head, Dock.Top); panel.Children.Add(head);
        var close = new Button { Content = "Close record", IsCancel = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        DockPanel.SetDock(close, Dock.Bottom); panel.Children.Add(close);
        var entries = new StackPanel();
        foreach (var status in Enum.GetValues<OutcomeStatus>())
        {
            var items = result.Outcomes.Where(o => o.Status == status).ToArray();
            entries.Children.Add(new TextBlock { Text = $"{status.ToString().ToUpperInvariant()} / {items.Length}", Foreground = (Brush)FindResource("Gold"), FontSize = 11, Margin = new Thickness(0, 14, 0, 10) });
            foreach (var item in items)
            {
                var content = new StackPanel();
                content.Children.Add(new TextBlock { Text = item.ApiId, FontWeight = FontWeights.SemiBold, FontSize = 13 });
                content.Children.Add(new TextBlock { Text = $"Accepted locally: {item.Accepted}   ·   Store acknowledged: {item.PersistenceAcknowledged}   ·   Verified: {item.Verified}", Foreground = (Brush)FindResource("Muted"), FontSize = 10, Margin = new Thickness(0, 7, 0, 7) });
                content.Children.Add(new TextBlock { Text = item.Message, FontSize = 11 });
                entries.Children.Add(new Border { Style = (Style)FindResource("Panel"), Padding = new Thickness(15), Margin = new Thickness(0, 0, 0, 8), Child = content });
            }
            if (items.Length == 0) entries.Children.Add(new TextBlock { Text = "No operations in this category.", Foreground = (Brush)FindResource("Muted"), FontSize = 11 });
        }
        panel.Children.Add(new ScrollViewer { Content = entries, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        root.Children.Add(panel); Content = root; Loaded += (_, _) => Motion.Reveal(root);
    }
    private static Border Counter(int count, string text, string color)
    {
        var content = new StackPanel();
        content.Children.Add(new TextBlock { Text = count.ToString(), FontSize = 32, Foreground = (Brush)new BrushConverter().ConvertFromString(color)! });
        content.Children.Add(new TextBlock { Text = text, FontSize = 9, Margin = new Thickness(0, 5, 0, 0) });
        return new Border { BorderBrush = (Brush)Application.Current.FindResource("Line"), BorderThickness = new Thickness(1), Padding = new Thickness(16), Margin = new Thickness(0, 0, 9, 0), Child = content };
    }
    private sealed class UniformGridCompat : System.Windows.Controls.Primitives.UniformGrid
    {
        public UniformGridCompat() { Columns = 3; }
    }
}

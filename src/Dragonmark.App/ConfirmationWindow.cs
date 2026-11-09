using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Dragonmark.App;

public sealed class ConfirmationWindow : Window
{
    public ConfirmationWindow(string title, string explanation, string targets, int count)
    {
        SetResourceReference(StyleProperty, typeof(Window));
        Title = "Review changes · Dragonmark"; Width = 720; Height = 760; MinWidth = 520; MinHeight = 550; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var background = new Grid { ClipToBounds = true };
        background.Children.Add(new NordicAtmosphere { IsHitTestVisible = false, Opacity = .55 });
        var layout = new DockPanel { Margin = new Thickness(30) };
        var head = new StackPanel();
        head.Children.Add(new TextBlock { Text = "DRAGONMARK  /  CONFIRMED BATCH", FontSize = 10, Foreground = (Brush)FindResource("Gold"), Margin = new Thickness(0, 0, 0, 16) });
        var heading = new TextBlock { Text = title, FontFamily = (FontFamily)FindResource("DisplayFont"), FontSize = 29, Margin = new Thickness(0, 0, 0, 18) };
        head.Children.Add(heading);
        var countPanel = new StackPanel { Orientation = Orientation.Horizontal };
        countPanel.Children.Add(new TextBlock { Text = count.ToString(), FontFamily = (FontFamily)FindResource("DisplayFont"), FontSize = 44, Foreground = (Brush)FindResource("Gold"), Margin = new Thickness(0, 0, 18, 0) });
        countPanel.Children.Add(new TextBlock { Text = "ACHIEVEMENTS\nPending your explicit confirmation", VerticalAlignment = VerticalAlignment.Center, FontSize = 11, Foreground = (Brush)FindResource("Muted") });
        head.Children.Add(new Border { BorderBrush = (Brush)FindResource("Line"), BorderThickness = new Thickness(1), Padding = new Thickness(18, 10, 18, 10), Child = countPanel, Margin = new Thickness(0, 0, 0, 18) });
        head.Children.Add(new TextBlock { Text = explanation, FontSize = 12, Margin = new Thickness(0, 0, 0, 18), Foreground = (Brush)FindResource("Frost") });
        head.Children.Add(new TextBlock { Text = "REVIEW EVERY TARGET", FontSize = 10, Foreground = (Brush)FindResource("Gold"), Margin = new Thickness(0, 0, 0, 10) });
        DockPanel.SetDock(head, Dock.Top); layout.Children.Add(head);
        var bottom = new StackPanel { Margin = new Thickness(0, 18, 0, 0) };
        bottom.Children.Add(new TextBlock { Text = "Accepted locally  →  Submitted  →  Verified / Failed / Unverified\nNo success is reported without persistence evidence. A timeout is not a rollback.", FontSize = 10, Foreground = (Brush)FindResource("Muted"), Margin = new Thickness(0, 0, 0, 17) });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Keep unchanged", IsCancel = true, IsDefault = true };
        var confirm = new Button { Content = $"Confirm {count} changes", Style = (Style)FindResource("PrimaryButton"), Margin = new Thickness(0) };
        confirm.Click += (_, _) => DialogResult = true; actions.Children.Add(cancel); actions.Children.Add(confirm);
        bottom.Children.Add(actions); DockPanel.SetDock(bottom, Dock.Bottom); layout.Children.Add(bottom);
        layout.Children.Add(new TextBox { Text = targets, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalContentAlignment = VerticalAlignment.Top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontSize = 12 });
        background.Children.Add(layout); Content = WindowLayout.Scrollable(background, 570, 650);
        WindowLayout.Attach(this);
        Loaded += (_, _) => { cancel.Focus(); Motion.Reveal(background); };
    }
}

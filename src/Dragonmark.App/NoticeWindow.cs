using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Dragonmark.App;

internal sealed class NoticeWindow : Window
{
    public NoticeWindow(string title, string message)
    {
        SetResourceReference(StyleProperty, typeof(Window));
        Title = title + " · Dragonmark"; Width = 560; SizeToContent = SizeToContent.Height; MinHeight = 240;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(26) };
        panel.Children.Add(new TextBlock { Text = title, FontFamily = (FontFamily)FindResource("DisplayFont"), FontSize = 24, Foreground = (Brush)FindResource("Gold"), Margin = new Thickness(0, 0, 0, 18) });
        panel.Children.Add(new TextBlock { Text = message, FontSize = 13 });
        var close = new Button { Content = "Understood", IsCancel = true, IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 24, 0, 0) };
        close.Click += (_, _) => Close(); panel.Children.Add(close); Content = panel;
        Loaded += (_, _) => Motion.Reveal(panel);
    }
}

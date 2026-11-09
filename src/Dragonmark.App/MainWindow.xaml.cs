using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Dragonmark.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _disposed;
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent(); _viewModel = viewModel; DataContext = viewModel;
        Width = Math.Min(Width, Math.Max(MinWidth, SystemParameters.WorkArea.Width - 32));
        Height = Math.Min(Height, Math.Max(MinHeight, SystemParameters.WorkArea.Height - 32));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (_, _) => SearchBox.Focus()));
        Closing += OnClosing;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.Page)) Motion.Reveal(PageContent);
            if (e.PropertyName is nameof(MainViewModel.Search) or nameof(MainViewModel.Filter)) Motion.Reveal(AchievementList);
        };
    }
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_disposed) return;
        e.Cancel = true;
        if (_viewModel.Busy)
        {
            new NoticeWindow("Operation in progress", "Wait for verification to finish, or cancel a read before closing. Steam writes cannot be rolled back by closing this window.") { Owner = this }.ShowDialog();
            return;
        }
        await _viewModel.DisposeAsync(); _disposed = true;
        _ = Dispatcher.BeginInvoke(Close);
    }
    public void SaveScreenshot(string path)
    {
        ScreenshotCapture.Save(this, path);
    }
}

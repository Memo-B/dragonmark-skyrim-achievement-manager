using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;

namespace Dragonmark.App;

internal static class WindowLayout
{
    public static void Attach(Window window)
    {
        double minimumWidth = window.MinWidth, minimumHeight = window.MinHeight;
        void Fit() => Constrain(window, WorkArea(window), minimumWidth, minimumHeight);
        window.SourceInitialized += (_, _) => Fit();
        window.DpiChanged += (_, _) => window.Dispatcher.BeginInvoke(Fit);
        window.StateChanged += (_, _) => { if (window.WindowState == WindowState.Normal) Fit(); };
        System.ComponentModel.PropertyChangedEventHandler changed = (_, _) => window.Dispatcher.BeginInvoke(Fit);
        SystemParameters.StaticPropertyChanged += changed;
        window.Closed += (_, _) => SystemParameters.StaticPropertyChanged -= changed;
    }

    internal static void Constrain(Window window, Rect workArea, double minimumWidth = 600, double minimumHeight = 360)
    {
        double width = Math.Max(1, workArea.Width - 24), height = Math.Max(1, workArea.Height - 24);
        // Lower the minimum before applying a smaller monitor's maximum.
        window.MinWidth = Math.Min(minimumWidth, width);
        window.MinHeight = Math.Min(minimumHeight, height);
        window.MaxWidth = width;
        window.MaxHeight = height;
        window.Width = Math.Clamp(double.IsNaN(window.Width) ? width : window.Width, window.MinWidth, width);
        window.Height = Math.Clamp(double.IsNaN(window.Height) ? height : window.Height, window.MinHeight, height);
    }

    public static ScrollViewer Scrollable(FrameworkElement content, double minimumWidth, double minimumHeight)
    {
        var viewport = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Focusable = false };
        var surface = new Grid { MinWidth = minimumWidth, MinHeight = minimumHeight };
        surface.SetBinding(FrameworkElement.WidthProperty, new Binding(nameof(ScrollViewer.ViewportWidth)) { Source = viewport });
        surface.SetBinding(FrameworkElement.HeightProperty, new Binding(nameof(ScrollViewer.ViewportHeight)) { Source = viewport });
        surface.Children.Add(content); viewport.Content = surface;
        return viewport;
    }

    private static Rect WorkArea(Window window)
    {
        nint handle = new WindowInteropHelper(window).Handle;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (handle != 0 && GetMonitorInfo(MonitorFromWindow(handle, 2), ref info))
        {
            uint dpi = GetDpiForWindow(handle);
            double scale = dpi == 0 ? 1 : dpi / 96d;
            return new Rect(info.Work.Left / scale, info.Work.Top / scale,
                (info.Work.Right - info.Work.Left) / scale, (info.Work.Bottom - info.Work.Top) / scale);
        }
        return SystemParameters.WorkArea;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);
}

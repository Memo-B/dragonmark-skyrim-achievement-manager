using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Dragonmark.App;

/// <summary>Original scalable mountain, mist and engraved-line artwork. No game files.</summary>
public sealed class NordicAtmosphere : FrameworkElement
{
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(new LinearGradientBrush(Color.FromRgb(10, 19, 29), Color.FromRgb(24, 43, 56), 90), null, new Rect(RenderSize));
        dc.PushTransform(new ScaleTransform(ActualWidth / 1500, ActualHeight / 900));
        var haze = new RadialGradientBrush(Color.FromArgb(48, 133, 171, 184), Colors.Transparent);
        dc.DrawEllipse(haze, null, new Point(1150, 205), 600, 370);
        dc.PushOpacity(.34);
        Draw(dc, "M 200,650 L 390,354 L 480,428 L 665,174 L 782,365 L 880,267 L 1030,455 L 1153,213 L 1320,425 L 1440,295 L 1500,390 L 1500,900 L 200,900 Z", "#263C49");
        Draw(dc, "M 480,428 L 665,174 L 782,365 L 703,329 L 672,255 L 638,318 L 597,324 Z", "#435966");
        Draw(dc, "M 1030,455 L 1153,213 L 1320,425 L 1218,357 L 1162,295 L 1125,336 L 1101,327 Z", "#3E5562");
        Draw(dc, "M 0,740 L 230,494 L 358,659 L 550,426 L 730,688 L 917,495 L 1102,682 L 1340,460 L 1500,629 L 1500,900 L 0,900 Z", "#192C39");
        Draw(dc, "M 0,810 L 260,655 L 423,790 L 615,653 L 830,812 L 1050,671 L 1290,795 L 1500,660 L 1500,900 L 0,900 Z", "#101F2B");
        dc.Pop();
        dc.DrawEllipse(new RadialGradientBrush(Color.FromArgb(22, 191, 214, 216), Colors.Transparent), null, new Point(900, 650), 760, 180);
        var engraving = new Pen(new SolidColorBrush(Color.FromArgb(35, 176, 191, 194)), 1);
        for (int i = 0; i < 8; i++)
        {
            double x = 1020 + i * 53;
            dc.DrawLine(engraving, new Point(x, 45), new Point(x + 18, 63));
            dc.DrawLine(engraving, new Point(x + 18, 63), new Point(x, 81));
        }
        dc.Pop();
    }
    private static void Draw(DrawingContext dc, string path, string color) => dc.DrawGeometry((Brush)new BrushConverter().ConvertFromString(color)!, null, Geometry.Parse(path));
}

public static class Motion
{
    public static readonly DependencyProperty PulsingProperty = DependencyProperty.RegisterAttached("Pulsing", typeof(bool), typeof(Motion), new PropertyMetadata(false, OnPulsing));
    public static bool GetPulsing(DependencyObject element) => (bool)element.GetValue(PulsingProperty);
    public static void SetPulsing(DependencyObject element, bool value) => element.SetValue(PulsingProperty, value);
    private static void OnPulsing(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not UIElement element) return;
        element.BeginAnimation(UIElement.OpacityProperty, null);
        element.Opacity = 1;
        if ((bool)args.NewValue && Allowed) element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(.45, 1, TimeSpan.FromMilliseconds(700)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
    }
    public static bool Reduced { get; set; }
    public static bool Allowed => !Reduced && SystemParameters.ClientAreaAnimation;
    public static void Reveal(UIElement element)
    {
        element.BeginAnimation(UIElement.OpacityProperty, null);
        element.Opacity = 1;
        if (Allowed) element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(.72, 1, TimeSpan.FromMilliseconds(160)) { FillBehavior = FillBehavior.Stop });
    }
}

/// <summary>Fictional demo illustrations; never used in place of an available Steam icon.</summary>
public static class DemoArtwork
{
    public static ImageSource Create(string id)
    {
        string path = id switch
        {
            "DEMO_HEARTH" => "M 14,31 L 32,14 L 50,31 M 19,28 L 19,49 L 45,49 L 45,28 M 29,49 L 29,35 L 36,35 L 36,49",
            "DEMO_IRON_OATH" => "M 32,10 L 38,19 L 34,40 L 43,42 L 41,46 L 34,45 L 34,54 L 30,54 L 30,45 L 23,46 L 21,42 L 30,40 L 26,19 Z",
            "DEMO_FIRST_LIGHT" => "M 32,12 L 32,20 M 32,44 L 32,52 M 12,32 L 20,32 M 44,32 L 52,32 M 18,18 L 23,23 M 41,41 L 46,46 M 18,46 L 23,41 M 41,23 L 46,18 M 32,23 A 9,9 0 1 0 32,41 A 9,9 0 1 0 32,23",
            "DEMO_SILENT_PEAK" => "M 9,49 L 30,13 L 54,49 Z M 21,30 L 28,33 L 32,25 L 39,33 M 17,49 L 28,32",
            "DEMO_LOST_ARCHIVE" => "M 12,17 L 26,17 L 32,21 L 38,17 L 52,17 L 52,46 L 38,46 L 32,50 L 26,46 L 12,46 Z M 32,21 L 32,50 M 18,25 L 26,25 M 18,31 L 26,31 M 38,25 L 46,25 M 38,31 L 46,31",
            "DEMO_WAYFARER" => "M 32,9 L 37,27 L 55,32 L 37,37 L 32,55 L 27,37 L 9,32 L 27,27 Z M 32,19 L 32,45 M 19,32 L 45,32",
            "DEMO_LAST_EMBER" => "M 32,10 C 44,24 52,34 45,45 C 37,57 19,52 17,39 C 15,30 22,22 24,20 C 24,30 31,29 32,10 Z M 32,33 C 43,44 32,52 27,43 Z",
            "DEMO_STAR_FORGED" => "M 32,9 L 38,25 L 55,26 L 42,37 L 47,54 L 32,44 L 17,54 L 22,37 L 9,26 L 26,25 Z",
            _ => "M 32,9 L 50,32 L 32,55 L 14,32 Z M 32,18 L 41,32 L 32,46 L 23,32 Z M 8,32 L 17,32 M 47,32 L 56,32"
        };
        var drawing = new DrawingGroup();
        using (var dc = drawing.Open())
        {
            dc.DrawRectangle(new LinearGradientBrush(Color.FromRgb(35, 54, 67), Color.FromRgb(12, 24, 34), 60), null, new Rect(0, 0, 64, 64));
            dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(78, 96, 105)), .6), new Point(32, 32), 27, 27);
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(210, 194, 153)), 1.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, Geometry.Parse(path));
        }
        drawing.Freeze();
        var image = new DrawingImage(drawing); image.Freeze(); return image;
    }
}

using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shell;
using Microsoft.Win32;

namespace WSiteFolderStart;

/// <summary>Colori e sfondo traslucido, che seguono il tema chiaro/scuro di Windows.</summary>
public static class Theme
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static readonly bool Dark = !IsLightTheme();

    static bool IsLightTheme() =>
        Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                          "AppsUseLightTheme", 1) is int v && v == 1;

    static SolidColorBrush Solid(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    static Color A(byte a, byte r, byte g, byte b) => Color.FromArgb(a, r, g, b);

    public static readonly Brush Fg = Dark ? Solid(Colors.White) : Solid(Color.FromRgb(30, 30, 30));
    public static readonly Brush Sub = Solid(Dark ? A(190, 255, 255, 255) : A(165, 0, 0, 0));
    public static readonly Brush Field = Solid(Dark ? A(40, 255, 255, 255) : A(120, 255, 255, 255));
    public static readonly Brush FieldStroke = Solid(Dark ? A(85, 255, 255, 255) : A(190, 255, 255, 255));
    public static readonly Brush Placeholder = Solid(Dark ? A(200, 255, 255, 255) : A(160, 0, 0, 0));
    public static readonly Brush Divider = Solid(Dark ? A(40, 255, 255, 255) : A(40, 0, 0, 0));
    public static readonly Color Blue = Color.FromRgb(0, 122, 255);
    public static readonly Color SolidBg = Dark ? Color.FromRgb(36, 36, 42) : Color.FromRgb(240, 240, 245);

    /// <summary>Riflesso diagonale della scheda di vetro.</summary>
    public static Brush CardFill() => new LinearGradientBrush(
        Dark ? A(42, 255, 255, 255) : A(110, 255, 255, 255), A(5, 255, 255, 255),
        new Point(0, 0), new Point(1, 1));

    /// <summary>Bordo luminoso della scheda di vetro.</summary>
    public static Brush CardStroke() => new LinearGradientBrush
    {
        StartPoint = new Point(0, 0),
        EndPoint = new Point(1, 1),
        GradientStops =
        {
            new GradientStop(Dark ? A(140, 255, 255, 255) : A(240, 255, 255, 255), 0),
            new GradientStop(A(16, 255, 255, 255), 0.5),
            new GradientStop(Dark ? A(72, 255, 255, 255) : A(140, 255, 255, 255), 1),
        }
    };

    /// <summary>Tinta blu per il pulsante principale.</summary>
    public static Brush PrimaryFill() => new LinearGradientBrush(
        Color.FromArgb(235, 70, 150, 255), Color.FromArgb(235, 0, 105, 235),
        new Point(0, 0), new Point(0, 1));

    public static void ApplyChrome(Window w, bool resizable)
    {
        WindowChrome.SetWindowChrome(w, new WindowChrome
        {
            CaptionHeight = 32,
            GlassFrameThickness = new Thickness(-1),
            ResizeBorderThickness = resizable ? new Thickness(6) : new Thickness(0),
            UseAeroCaptionButtons = true
        });
    }

    public static void ApplyBackdrop(Window w)
    {
        var h = new WindowInteropHelper(w).Handle;
        int useDark = Dark ? 1 : 0;
        DwmSetWindowAttribute(h, 20, ref useDark, sizeof(int));          // titolo scuro/chiaro

        if (Environment.OSVersion.Version.Build >= 22621)                // Windows 11 22H2 o successivo
        {
            int acrylic = 3;                                             // 3 = Acrylic, 2 = Mica
            DwmSetWindowAttribute(h, 38, ref acrylic, sizeof(int));
            w.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        }
        else                                                             // Windows 10 / 11 vecchio: sfondo pieno
        {
            w.Background = new SolidColorBrush(SolidBg);
        }
    }
}

/// <summary>Controlli "di vetro" costruiti a codice (TextBox, pulsanti, menu).</summary>
public static class Ui
{
    public static readonly FontFamily Font = new FontFamily("Segoe UI Variable Display, Segoe UI");
    static readonly FontFamily IconFont = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");

    const string Ns = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                      "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";

    static string R(double r) => r.ToString(CultureInfo.InvariantCulture);

    public static TextBlock Text(string text, double size = 14, Brush? fg = null, bool bold = false) => new()
    {
        Text = text,
        FontSize = size,
        Foreground = fg ?? Theme.Fg,
        FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
        TextWrapping = TextWrapping.Wrap
    };

    public static TextBlock Glyph(string code, double size = 14, Brush? fg = null) => new()
    {
        Text = code,
        FontFamily = IconFont,
        FontSize = size,
        Foreground = fg ?? Theme.Fg,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    public static TextBox Input(double radius, double height, bool multiline = false)
    {
        var valign = multiline ? "Stretch" : "Center";
        var template = (ControlTemplate)XamlReader.Parse(
            $"<ControlTemplate {Ns} TargetType=\"TextBox\">" +
            "<Border Background=\"{TemplateBinding Background}\" BorderBrush=\"{TemplateBinding BorderBrush}\" " +
            $"BorderThickness=\"{{TemplateBinding BorderThickness}}\" CornerRadius=\"{R(radius)}\" Padding=\"{{TemplateBinding Padding}}\">" +
            $"<ScrollViewer x:Name=\"PART_ContentHost\" Focusable=\"False\" VerticalAlignment=\"{valign}\" " +
            "HorizontalScrollBarVisibility=\"{TemplateBinding HorizontalScrollBarVisibility}\" " +
            "VerticalScrollBarVisibility=\"{TemplateBinding VerticalScrollBarVisibility}\"/>" +
            "</Border></ControlTemplate>");

        var tb = new TextBox
        {
            Template = template,
            Foreground = Theme.Fg,
            CaretBrush = Theme.Fg,
            Background = Theme.Field,
            BorderBrush = Theme.FieldStroke,
            BorderThickness = new Thickness(1),
            Padding = multiline ? new Thickness(8) : new Thickness(12, 0, 12, 0),
            FontSize = 14
        };
        if (!double.IsNaN(height)) tb.Height = height;
        return tb;
    }

    static ControlTemplate ButtonTemplate(double radius) => (ControlTemplate)XamlReader.Parse(
        $"<ControlTemplate {Ns} TargetType=\"Button\">" +
        "<Border x:Name=\"root\" Background=\"{TemplateBinding Background}\" BorderBrush=\"{TemplateBinding BorderBrush}\" " +
        $"BorderThickness=\"{{TemplateBinding BorderThickness}}\" CornerRadius=\"{R(radius)}\" Padding=\"{{TemplateBinding Padding}}\">" +
        "<ContentPresenter HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"/></Border>" +
        "<ControlTemplate.Triggers>" +
        "<Trigger Property=\"IsMouseOver\" Value=\"True\"><Setter TargetName=\"root\" Property=\"Opacity\" Value=\"0.88\"/></Trigger>" +
        "<Trigger Property=\"IsPressed\" Value=\"True\"><Setter TargetName=\"root\" Property=\"Opacity\" Value=\"0.7\"/></Trigger>" +
        "<Trigger Property=\"IsEnabled\" Value=\"False\"><Setter TargetName=\"root\" Property=\"Opacity\" Value=\"0.5\"/></Trigger>" +
        "</ControlTemplate.Triggers></ControlTemplate>");

    static Button MakeButton(object content, double radius, Brush bg, Brush border, Brush fg, Thickness padding) => new()
    {
        Content = content,
        Template = ButtonTemplate(radius),
        Background = bg,
        BorderBrush = border,
        BorderThickness = new Thickness(1),
        Foreground = fg,
        Padding = padding,
        FontSize = 14,
        Cursor = System.Windows.Input.Cursors.Hand
    };

    /// <summary>Pulsante di vetro (secondario).</summary>
    public static Button Glass(object content, double radius, Thickness padding)
    {
        var b = MakeButton(content, radius, Theme.Field, Theme.FieldStroke, Theme.Fg, padding);
        b.Effect = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 4, Direction = 270, Opacity = Theme.Dark ? 0.3 : 0.12, Color = Colors.Black };
        return b;
    }

    /// <summary>Pulsante principale, tinto di blu.</summary>
    public static Button Primary(object content, double radius, Thickness padding)
    {
        var b = MakeButton(content, radius, Theme.PrimaryFill(), Theme.CardStroke(), Brushes.White, padding);
        b.FontWeight = FontWeights.SemiBold;
        b.Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 6, Direction = 270, Opacity = Theme.Dark ? 0.4 : 0.2, Color = Colors.Black };
        return b;
    }

    /// <summary>Piccolo pulsante con una sola icona, senza sfondo.</summary>
    public static Button IconButton(string glyph, string tip)
    {
        var b = MakeButton(Glyph(glyph, 14, Theme.Sub), 8, Brushes.Transparent, Brushes.Transparent, Theme.Fg, new Thickness(0));
        b.Width = 30;
        b.Height = 30;
        b.ToolTip = tip;
        return b;
    }

    /// <summary>Apre un menu a tendina sotto il controllo indicato. Un titolo "-" è un separatore.</summary>
    public static void Menu(FrameworkElement target, params (string Header, bool Checked, Action Do)[] items)
    {
        var m = new ContextMenu { PlacementTarget = target, Placement = PlacementMode.Bottom };
        foreach (var it in items)
        {
            if (it.Header == "-") { m.Items.Add(new Separator()); continue; }
            var mi = new MenuItem { Header = it.Header, IsChecked = it.Checked };
            var action = it.Do;
            mi.Click += (_, _) => action();
            m.Items.Add(mi);
        }
        m.IsOpen = true;
    }
}

using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Microsoft.Win32;

namespace WSiteFolderStart;

public class App : Application
{
    [STAThread]
    public static void Main() => new App().Run(new MainWindow());
}

public class MainWindow : Window
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    readonly TextBox input = new();
    readonly TextBlock error = new();
    readonly TextBlock pathText = new();
    readonly TextBlock placeholder = new();
    readonly bool dark = !IsLightTheme();

    public MainWindow()
    {
        Title = "WSiteFolderStart";
        Width = 380; Height = 500;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brushes.Transparent;
        Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/icon.png"));
        WindowChrome.SetWindowChrome(this, new WindowChrome {
            CaptionHeight = 32, GlassFrameThickness = new Thickness(-1),
            ResizeBorderThickness = new Thickness(0), UseAeroCaptionButtons = true });

        var fg = dark ? Brushes.White : new SolidColorBrush(Color.FromRgb(30, 30, 30));
        var sub = new SolidColorBrush(dark ? Color.FromArgb(170, 255, 255, 255) : Color.FromArgb(150, 0, 0, 0));
        var glass = new SolidColorBrush(dark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(110, 255, 255, 255));
        var stroke = new SolidColorBrush(dark ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(140, 255, 255, 255));

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center,
                                     HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 24, 0, 0) };
        stack.Children.Add(new System.Windows.Controls.Image {
            Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/icon.png")),
            Width = 64, Height = 64, Margin = new Thickness(0, 0, 0, 14) });
        stack.Children.Add(new TextBlock { Text = "App in esecuzione", FontSize = 22, FontWeight = FontWeights.SemiBold,
            Foreground = fg, HorizontalAlignment = HorizontalAlignment.Center });
        stack.Children.Add(new TextBlock { Text = "Inserisci nome cartella principale", FontSize = 13, Foreground = sub,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 14) });

        input.Width = 260; input.Height = 38; input.FontSize = 14; input.Foreground = fg;
        input.CaretBrush = fg; input.HorizontalContentAlignment = HorizontalAlignment.Center;
        input.VerticalContentAlignment = VerticalAlignment.Center;
        input.Background = glass; input.BorderBrush = stroke;
        input.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) Scatena(); };
        placeholder.Text = "es. MioSito"; placeholder.FontSize = 14;
        placeholder.Foreground = new SolidColorBrush(dark ? Color.FromArgb(200, 255, 255, 255) : Color.FromArgb(160, 0, 0, 0));
        placeholder.IsHitTestVisible = false;
        placeholder.HorizontalAlignment = HorizontalAlignment.Center;
        placeholder.VerticalAlignment = VerticalAlignment.Center;
        var host = new Grid { Width = 260 };
        host.Children.Add(input);
        host.Children.Add(placeholder);
        stack.Children.Add(host);

        pathText.FontSize = 11; pathText.Foreground = sub; pathText.TextWrapping = TextWrapping.Wrap;
        pathText.MaxWidth = 300; pathText.TextAlignment = TextAlignment.Center;
        pathText.HorizontalAlignment = HorizontalAlignment.Center; pathText.Margin = new Thickness(0, 10, 0, 0);
        stack.Children.Add(pathText);
        input.TextChanged += (_, _) => UpdatePath();
        UpdatePath();

        error.Foreground = Brushes.IndianRed; error.FontSize = 12;
        error.HorizontalAlignment = HorizontalAlignment.Center; error.Margin = new Thickness(0, 8, 0, 0);
        stack.Children.Add(error);

        var btn = new Button { Content = "Scatena", Width = 140, Height = 38, FontSize = 14,
            FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 16, 0, 0),
            Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0, 122, 255)),
            BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        btn.Click += (_, _) => Scatena();
        stack.Children.Add(btn);

        Content = stack;
        Loaded += (_, _) => { ApplyBackdrop(); input.Focus(); };
    }

    static bool IsLightTheme() =>
        Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                          "AppsUseLightTheme", 1) is int v && v == 1;

    void ApplyBackdrop()
    {
        var h = new WindowInteropHelper(this).Handle;
        int d = dark ? 1 : 0, mica = 3; // 3 = Acrylic/"Mica Alt" a seconda della build di Windows 11
        DwmSetWindowAttribute(h, 20, ref d, sizeof(int));     // tema scuro/chiaro
        DwmSetWindowAttribute(h, 38, ref mica, sizeof(int));  // backdrop traslucido
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
    }

    static string Clean(string t) =>
        string.Concat(t.Trim().Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '-' : c));

    void UpdatePath()
    {
        placeholder.Visibility = input.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var name = Clean(input.Text);
        pathText.Text = "La cartella verrà creata in:\n" + Path.Combine(desktop, name.Length == 0 ? "…" : name);
    }

    void Scatena()
    {
        var name = Clean(input.Text);
        if (name.Length == 0) return;
        try
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name);
            void W(string folder, string file, string text)
            {
                var dir = Path.Combine(root, folder); Directory.CreateDirectory(dir);
                var p = Path.Combine(dir, file); if (!File.Exists(p)) File.WriteAllText(p, text);
            }
            W("Html", "index.html", $"<!DOCTYPE html>\n<html lang=\"it\">\n<head>\n  <meta charset=\"UTF-8\">\n  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n  <title>{name}</title>\n  <link rel=\"stylesheet\" href=\"../Css/style.css\">\n</head>\n<body>\n  <h1>{name}</h1>\n  <script src=\"../Js/script.js\"></script>\n</body>\n</html>\n");
            W("Css", "style.css", $"/* Stili di {name} */\n");
            W("Php", "login.php", $"<?php\n// Login di {name}\n");
            W("Js", "script.js", $"// Script di {name}\n");
            Directory.CreateDirectory(Path.Combine(root, "Img"));
            Application.Current.Shutdown();
        }
        catch (Exception ex) { error.Text = "Errore: " + ex.Message; }
    }
}

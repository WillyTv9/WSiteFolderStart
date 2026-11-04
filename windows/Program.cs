using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace WSiteFolderStart;

public class App : Application
{
    [STAThread]
    public static void Main()
    {
        var app = new App();
        var store = Store.Load();
        app.Run(new MainWindow(store));
    }
}

public class MainWindow : Window
{
    readonly Store store;
    readonly TextBox input = Ui.Input(19, 38);
    readonly TextBlock placeholder = Ui.Text("es. MioSito", 14, Theme.Placeholder);
    readonly TextBlock presetLabel = Ui.Text("", 13);
    readonly TextBlock pathText = Ui.Text("", 11, Theme.Sub);
    readonly TextBlock error = Ui.Text("", 12, Brushes.IndianRed);
    readonly Button presetBtn;
    readonly Button goBtn;
    EditorWindow? editor;

    public MainWindow(Store store)
    {
        this.store = store;

        Title = "WSiteFolderStart";
        Width = 400;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brushes.Transparent;
        FontFamily = Ui.Font;
        Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/icon.png"));
        Theme.ApplyChrome(this, false);

        // ── Titolo e icona
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        stack.Children.Add(new System.Windows.Controls.Image
        {
            Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/icon.png")),
            Width = 68, Height = 68, Margin = new Thickness(0, 0, 0, 14),
            Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 4, Direction = 270, Opacity = 0.25, Color = Colors.Black }
        });

        var title = Ui.Text("App in esecuzione", 22, Theme.Fg, true);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(title);

        var subtitle = Ui.Text("Inserisci nome cartella principale", 13, Theme.Sub);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.Margin = new Thickness(0, 8, 0, 14);
        stack.Children.Add(subtitle);

        // ── Campo del nome, con placeholder leggibile
        input.TextAlignment = TextAlignment.Center;
        placeholder.IsHitTestVisible = false;
        placeholder.HorizontalAlignment = HorizontalAlignment.Center;
        placeholder.VerticalAlignment = VerticalAlignment.Center;
        var host = new Grid { Width = 250 };
        host.Children.Add(input);
        host.Children.Add(placeholder);
        stack.Children.Add(host);

        // ── Preset + gestione preset
        var presetContent = new StackPanel { Orientation = Orientation.Horizontal };
        presetContent.Children.Add(Ui.Glyph("\uE81E", 13));
        presetLabel.Margin = new Thickness(8, 0, 6, 0);
        presetLabel.TextTrimming = TextTrimming.CharacterEllipsis;
        presetLabel.MaxWidth = 130;
        presetLabel.TextWrapping = TextWrapping.NoWrap;
        presetContent.Children.Add(presetLabel);
        presetContent.Children.Add(Ui.Text("▾", 11, Theme.Sub));

        presetBtn = Ui.Glass(presetContent, 18, new Thickness(14, 7, 14, 7));
        presetBtn.Click += (_, _) => ShowPresetMenu();

        var manageBtn = Ui.Glass(Ui.Glyph("\uE713", 14), 18, new Thickness(0));
        manageBtn.Width = 36;
        manageBtn.Height = 36;
        manageBtn.ToolTip = "Gestisci preset e IDE";
        manageBtn.Click += (_, _) => OpenEditor();

        var presetRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 14, 0, 0)
        };
        presetRow.Children.Add(presetBtn);
        manageBtn.Margin = new Thickness(8, 0, 0, 0);
        presetRow.Children.Add(manageBtn);
        stack.Children.Add(presetRow);

        // ── Percorso (solo su Windows) ed errori
        pathText.TextAlignment = TextAlignment.Center;
        pathText.MaxWidth = 280;
        pathText.HorizontalAlignment = HorizontalAlignment.Center;
        pathText.Margin = new Thickness(0, 12, 0, 0);
        stack.Children.Add(pathText);

        error.TextAlignment = TextAlignment.Center;
        error.MaxWidth = 280;
        error.HorizontalAlignment = HorizontalAlignment.Center;
        error.Margin = new Thickness(0, 6, 0, 0);
        stack.Children.Add(error);

        // ── Scatena
        goBtn = Ui.Primary("Scatena", 19, new Thickness(0));
        goBtn.Width = 150;
        goBtn.Height = 38;
        goBtn.Margin = new Thickness(0, 16, 0, 0);
        goBtn.Click += (_, _) => Go();
        stack.Children.Add(goBtn);

        // ── Scheda di vetro sopra le macchie di colore
        var card = new Border
        {
            Margin = new Thickness(28, 44, 28, 28),
            Padding = new Thickness(30),
            CornerRadius = new CornerRadius(36),
            Background = Theme.CardFill(),
            BorderBrush = Theme.CardStroke(),
            BorderThickness = new Thickness(1.2),
            Child = stack,
            Effect = new DropShadowEffect
            {
                BlurRadius = 28, ShadowDepth = 9, Direction = 270,
                Opacity = Theme.Dark ? 0.35 : 0.15, Color = Colors.Black
            }
        };

        var root = new Grid();
        root.Children.Add(Blobs());
        root.Children.Add(card);
        Content = root;

        input.TextChanged += (_, _) => { error.Text = ""; Refresh(); };
        input.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) Go(); };
        store.Changed += Refresh;

        Loaded += (_, _) => { Theme.ApplyBackdrop(this); input.Focus(); };
        Refresh();
    }

    /// <summary>Macchie di colore sfocate che si muovono lentamente dietro al vetro.</summary>
    static UIElement Blobs()
    {
        var inner = new Grid { IsHitTestVisible = false, Opacity = 0.55, Effect = new BlurEffect { Radius = 55 } };
        inner.Children.Add(Blob(Color.FromRgb(64, 191, 204), 220, -50, -80, -110, -130));
        inner.Children.Add(Blob(Color.FromRgb(224, 115, 184), 200, 60, 130, 110, 80));
        inner.Children.Add(Blob(Color.FromRgb(140, 128, 235), 180, 120, -60, 70, -110));
        return new Grid { ClipToBounds = true, IsHitTestVisible = false, Children = { inner } };
    }

    static UIElement Blob(Color color, double size, double x1, double y1, double x2, double y2)
    {
        var tr = new TranslateTransform(x1, y1);
        var e = new System.Windows.Shapes.Ellipse
        {
            Width = size, Height = size, Fill = new SolidColorBrush(color),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransform = tr
        };
        DoubleAnimation Anim(double from, double to) => new(from, to, new Duration(TimeSpan.FromSeconds(7)))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        tr.BeginAnimation(TranslateTransform.XProperty, Anim(x1, x2));
        tr.BeginAnimation(TranslateTransform.YProperty, Anim(y1, y2));
        return e;
    }

    void ShowPresetMenu()
    {
        var items = store.Presets
            .Select(p => (p.Name, p.Id == store.SelectedId, (Action)(() =>
            {
                store.SelectedId = p.Id;
                store.Touch();
            })))
            .ToArray();
        Ui.Menu(presetBtn, items);
    }

    void OpenEditor()
    {
        if (editor != null) { editor.Activate(); return; }
        editor = new EditorWindow(store) { Owner = this };
        editor.Closed += (_, _) => editor = null;
        editor.Show();
    }

    /// <summary>Aggiorna etichetta del preset, pulsante e percorso in base a nome e preset scelti.</summary>
    void Refresh()
    {
        var p = store.Selected;
        var name = Builder.Safe(input.Text);

        presetLabel.Text = p.Name;
        placeholder.Visibility = input.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        goBtn.IsEnabled = name.Length > 0;

        var dest = Builder.ResolveDestination(p.Destination);
        var text = "La cartella verrà creata in:\n" + System.IO.Path.Combine(dest, name.Length == 0 ? "…" : name);
        if (!string.IsNullOrWhiteSpace(p.IdePath))
            text += "\ne poi aperta in " + Ide.NameOf(p.IdePath);
        pathText.Text = text;
    }

    void Go()
    {
        var name = Builder.Safe(input.Text);
        if (name.Length == 0) return;
        var p = store.Selected;

        string root;
        try { root = Builder.Create(name, p); }
        catch (Exception ex) { error.Text = "Errore: " + ex.Message; return; }

        if (!string.IsNullOrWhiteSpace(p.IdePath))
        {
            try { Ide.Open(p.IdePath, root); }
            catch
            {
                error.Text = "Cartella creata, ma non riesco ad aprire l'IDE. Controlla la scelta nel preset.";
                return;
            }
        }
        Application.Current.Shutdown();
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace WSiteFolderStart;

/// <summary>Finestra "Preset e IDE": crea, modifica ed elimina preset e la loro struttura.</summary>
public class EditorWindow : Window
{
    readonly Store store;
    readonly ListBox list = new();
    readonly Border detailHost = new();
    bool suppress;
    Preset? current;
    StackPanel? tree;

    public EditorWindow(Store store)
    {
        this.store = store;

        Title = "Preset e IDE";
        Width = 920;
        Height = 660;
        MinWidth = 780;
        MinHeight = 540;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.Transparent;
        FontFamily = Ui.Font;
        Theme.ApplyChrome(this, true);
        Loaded += (_, _) => Theme.ApplyBackdrop(this);

        // ── Colonna sinistra: elenco preset + pulsanti
        list.Background = Brushes.Transparent;
        list.BorderThickness = new Thickness(0);
        list.Foreground = Theme.Fg;
        list.FontSize = 14;
        list.Margin = new Thickness(8);
        list.SelectionChanged += (_, _) =>
        {
            if (!suppress) Select(list.SelectedIndex);
        };

        var addBtn = Ui.IconButton("\uE710", "Nuovo preset");
        addBtn.Click += (_, _) =>
        {
            var i = store.Add();
            RefreshList(i);
            Select(i);
        };
        var dupBtn = Ui.IconButton("\uE8C8", "Duplica preset");
        dupBtn.Click += (_, _) =>
        {
            if (list.SelectedIndex < 0) return;
            var i = store.Duplicate(list.SelectedIndex);
            RefreshList(i);
            Select(i);
        };
        var delBtn = Ui.IconButton("\uE738", "Elimina preset");
        delBtn.Click += (_, _) =>
        {
            var idx = list.SelectedIndex;
            if (idx < 0) return;
            store.Delete(idx);
            var next = Math.Min(idx, store.Presets.Count - 1);
            RefreshList(next);
            Select(next);
        };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 4, 10, 10) };
        buttons.Children.Add(addBtn);
        buttons.Children.Add(dupBtn);
        buttons.Children.Add(delBtn);

        var sidebar = new Grid();
        sidebar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        sidebar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        sidebar.Children.Add(list);
        Grid.SetRow(buttons, 1);
        sidebar.Children.Add(buttons);

        // ── Layout
        var root = new Grid { Margin = new Thickness(0, 34, 0, 0) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var divider = new Border { Background = Theme.Divider };
        Grid.SetColumn(divider, 1);
        Grid.SetColumn(detailHost, 2);
        root.Children.Add(sidebar);
        root.Children.Add(divider);
        root.Children.Add(detailHost);
        Content = root;

        var start = Math.Max(0, store.Presets.FindIndex(p => p.Id == store.SelectedId));
        RefreshList(start);
        Select(start);
    }

    void RefreshList(int select)
    {
        suppress = true;
        list.Items.Clear();
        foreach (var p in store.Presets) list.Items.Add(p.Name);
        list.SelectedIndex = select;
        suppress = false;
    }

    void Select(int i)
    {
        if (i < 0 || i >= store.Presets.Count)
        {
            current = null;
            detailHost.Child = null;
            return;
        }
        current = store.Presets[i];
        detailHost.Child = BuildDetail(current, i);
    }

    // ───────────── Dettaglio del preset selezionato ─────────────

    UIElement BuildDetail(Preset p, int index)
    {
        var panel = new StackPanel { Margin = new Thickness(24) };

        // Nome
        panel.Children.Add(Ui.Text("Nome del preset", 15, Theme.Fg, true));
        var nameBox = Ui.Input(8, 32);
        nameBox.Text = p.Name;
        nameBox.Margin = new Thickness(0, 6, 0, 20);
        nameBox.TextChanged += (_, _) =>
        {
            p.Name = nameBox.Text;
            store.Touch();
            RefreshList(index);
        };
        panel.Children.Add(nameBox);

        // Destinazione
        panel.Children.Add(Ui.Text("Dove creare la cartella", 15, Theme.Fg, true));
        var destBox = Ui.Input(8, 32);
        destBox.Text = p.Destination;
        destBox.FontFamily = new FontFamily("Consolas");
        var chooseBtn = Ui.Glass("Scegli…", 8, new Thickness(14, 0, 14, 0));
        chooseBtn.Height = 32;
        chooseBtn.Margin = new Thickness(8, 0, 0, 0);
        var destRow = new Grid { Margin = new Thickness(0, 6, 0, 0) };
        destRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        destRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(chooseBtn, 1);
        destRow.Children.Add(destBox);
        destRow.Children.Add(chooseBtn);
        panel.Children.Add(destRow);

        var pathCaption = Ui.Text("", 11, Theme.Sub);
        pathCaption.Margin = new Thickness(0, 6, 0, 0);
        panel.Children.Add(pathCaption);
        var hint = Ui.Text("Puoi usare ~ (cartella utente), %DESKTOP% e %DOCUMENTS%.", 11, Theme.Sub);
        hint.Margin = new Thickness(0, 2, 0, 20);
        panel.Children.Add(hint);

        void UpdateCaption() =>
            pathCaption.Text = "Verrà creata in: " + System.IO.Path.Combine(Builder.ResolveDestination(p.Destination), "NomeScelto");
        UpdateCaption();

        destBox.TextChanged += (_, _) =>
        {
            p.Destination = destBox.Text;
            store.Touch();
            UpdateCaption();
        };
        chooseBtn.Click += (_, _) =>
        {
            var dlg = new OpenFolderDialog { Title = "Scegli la cartella di destinazione" };
            if (dlg.ShowDialog(this) == true) destBox.Text = dlg.FolderName;
        };

        // IDE
        panel.Children.Add(Ui.Text("IDE con cui aprire il progetto", 15, Theme.Fg, true));
        var ideLabel = Ui.Text("", 14);
        ideLabel.TextWrapping = TextWrapping.NoWrap;
        var ideBtn = Ui.Glass(ideLabel, 8, new Thickness(14, 0, 14, 0));
        ideBtn.Height = 32;
        ideBtn.HorizontalAlignment = HorizontalAlignment.Left;
        ideBtn.Margin = new Thickness(0, 6, 0, 0);
        void UpdateIde() =>
            ideLabel.Text = (string.IsNullOrWhiteSpace(p.IdePath) ? "Nessuno" : Ide.NameOf(p.IdePath)) + "  ▾";
        UpdateIde();
        void SetIde(string? path)
        {
            p.IdePath = path;
            store.Touch();
            UpdateIde();
        }
        ideBtn.Click += (_, _) =>
        {
            var items = new List<(string, bool, Action)>
            {
                ("Nessuno", string.IsNullOrWhiteSpace(p.IdePath), () => SetIde(null))
            };
            foreach (var ide in Ide.Installed())
            {
                var path = ide.Path;
                items.Add((ide.Name, string.Equals(p.IdePath, path, StringComparison.OrdinalIgnoreCase), () => SetIde(path)));
            }
            items.Add(("-", false, () => { }));
            items.Add(("Altra app…", false, () =>
            {
                var dlg = new OpenFileDialog { Title = "Scegli l'IDE", Filter = "Applicazioni (*.exe)|*.exe" };
                if (dlg.ShowDialog(this) == true) SetIde(dlg.FileName);
            }));
            Ui.Menu(ideBtn, items.ToArray());
        };
        panel.Children.Add(ideBtn);

        var ideHint = Ui.Text("Se è impostato, dopo «Scatena» l'app apre la cartella principale in questo IDE (non nel browser) e si chiude.", 11, Theme.Sub);
        ideHint.Margin = new Thickness(0, 6, 0, 22);
        panel.Children.Add(ideHint);

        // Struttura
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(Ui.Text("Struttura", 15, Theme.Fg, true));
        var addRoot = Ui.Glass(Ui.Text("+  Aggiungi", 13), 8, new Thickness(12, 0, 12, 0));
        addRoot.Height = 28;
        addRoot.Click += (_, _) => Ui.Menu(addRoot,
            ("Nuova cartella", false, () => { p.Items.Add(Node.NewFolder("nuova-cartella")); store.Touch(); RebuildTree(); }),
            ("Nuovo file", false, () => { p.Items.Add(Node.NewFile("nuovo-file.txt")); store.Touch(); RebuildTree(); }));
        Grid.SetColumn(addRoot, 1);
        header.Children.Add(addRoot);
        panel.Children.Add(header);

        var rootRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 8) };
        rootRow.Children.Add(Ui.Glyph("\uE8B7", 15, new SolidColorBrush(Theme.Blue)));
        var rootName = Ui.Text("{{nome}}", 14);
        rootName.FontFamily = new FontFamily("Consolas");
        rootName.Margin = new Thickness(8, 0, 8, 0);
        rootRow.Children.Add(rootName);
        rootRow.Children.Add(Ui.Text("cartella principale", 13, Theme.Sub));
        panel.Children.Add(rootRow);

        tree = new StackPanel { Margin = new Thickness(22, 0, 0, 0) };
        panel.Children.Add(tree);
        RebuildTree();

        var note = Ui.Text("Nei nomi e nel contenuto dei file puoi usare {{nome}} (il nome scritto nella finestra principale), {{data}} e {{anno}}. " +
                           "Una cartella senza elementi viene creata vuota, un file senza contenuto viene creato vuoto.", 11, Theme.Sub);
        note.Margin = new Thickness(0, 20, 0, 0);
        panel.Children.Add(note);

        return new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    // ───────────── Albero di cartelle e file ─────────────

    void RebuildTree()
    {
        if (tree == null || current == null) return;
        tree.Children.Clear();
        foreach (var n in current.Items.ToList())
            tree.Children.Add(BuildNode(n, current.Items));
    }

    UIElement BuildNode(Node node, List<Node> siblings)
    {
        var box = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = node.IsFolder
            ? Ui.Glyph("\uE8B7", 15, new SolidColorBrush(Theme.Blue))
            : Ui.Glyph("\uE7C3", 15, Theme.Sub);
        row.Children.Add(icon);

        var nameBox = Ui.Input(8, 30);
        nameBox.Text = node.Name;
        nameBox.TextChanged += (_, _) =>
        {
            node.Name = nameBox.Text;
            store.Touch();
        };
        Grid.SetColumn(nameBox, 1);
        row.Children.Add(nameBox);

        TextBox? contentBox = null;
        if (node.IsFolder)
        {
            var plus = Ui.IconButton("\uE710", "Aggiungi dentro questa cartella");
            plus.Click += (_, _) => Ui.Menu(plus,
                ("Nuova cartella", false, () => { node.Children.Add(Node.NewFolder("nuova-cartella")); store.Touch(); RebuildTree(); }),
                ("Nuovo file", false, () => { node.Children.Add(Node.NewFile("nuovo-file.txt")); store.Touch(); RebuildTree(); }));
            Grid.SetColumn(plus, 2);
            row.Children.Add(plus);
        }
        else
        {
            var edit = Ui.IconButton("\uE70F", "Modifica il contenuto del file");
            edit.Click += (_, _) =>
            {
                if (contentBox != null)
                    contentBox.Visibility = contentBox.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            };
            Grid.SetColumn(edit, 2);
            row.Children.Add(edit);
        }

        var trash = Ui.IconButton("\uE74D", "Elimina");
        trash.Click += (_, _) =>
        {
            siblings.Remove(node);
            store.Touch();
            RebuildTree();
        };
        Grid.SetColumn(trash, 3);
        row.Children.Add(trash);
        box.Children.Add(row);

        if (node.IsFolder)
        {
            var children = new StackPanel { Margin = new Thickness(22, 6, 0, 0) };
            foreach (var child in node.Children.ToList())
                children.Children.Add(BuildNode(child, node.Children));
            box.Children.Add(children);
        }
        else
        {
            contentBox = Ui.Input(8, double.NaN, multiline: true);
            contentBox.Height = 140;
            contentBox.AcceptsReturn = true;
            contentBox.AcceptsTab = true;
            contentBox.FontFamily = new FontFamily("Consolas");
            contentBox.FontSize = 13;
            contentBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            contentBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            contentBox.Text = node.Content;
            contentBox.Margin = new Thickness(24, 6, 0, 0);
            contentBox.Visibility = Visibility.Collapsed;
            var cb = contentBox;
            cb.TextChanged += (_, _) =>
            {
                node.Content = cb.Text;
                store.Touch();
            };
            box.Children.Add(cb);
        }

        return box;
    }
}

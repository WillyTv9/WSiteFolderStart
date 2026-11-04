using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace WSiteFolderStart;

// ───────────── Modelli ─────────────

/// <summary>Un elemento della struttura: cartella (con figli) oppure file (con contenuto).</summary>
public class Node
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public bool IsFolder { get; set; }
    public string Content { get; set; } = "";
    public List<Node> Children { get; set; } = new();

    public static Node NewFolder(string name, params Node[] children) =>
        new() { Name = name, IsFolder = true, Children = children.ToList() };

    public static Node NewFile(string name, string content = "") =>
        new() { Name = name, IsFolder = false, Content = content };
}

/// <summary>Un preset: nome, cartella di destinazione, IDE e struttura da creare.</summary>
public class Preset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Nuovo preset";
    public string Destination { get; set; } = "%DESKTOP%";
    public string? IdePath { get; set; }
    public List<Node> Items { get; set; } = new();

    const string HtmlTemplate =
        "<!DOCTYPE html>\n<html lang=\"it\">\n<head>\n  <meta charset=\"UTF-8\">\n" +
        "  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n" +
        "  <title>{{nome}}</title>\n  <link rel=\"stylesheet\" href=\"../Css/style.css\">\n</head>\n" +
        "<body>\n  <h1>{{nome}}</h1>\n  <script src=\"../Js/script.js\"></script>\n</body>\n</html>\n";

    public static Preset Default() => new()
    {
        Name = "Sito Web",
        Items = new List<Node>
        {
            Node.NewFolder("Html", Node.NewFile("index.html", HtmlTemplate)),
            Node.NewFolder("Css", Node.NewFile("style.css", "/* Stili di {{nome}} */\n")),
            Node.NewFolder("Php", Node.NewFile("login.php", "<?php\n// Login di {{nome}}\n")),
            Node.NewFolder("Js", Node.NewFile("script.js", "// Script di {{nome}}\n")),
            Node.NewFolder("Img"),
        }
    };
}

public class StoreData
{
    public Guid SelectedId { get; set; }
    public List<Preset> Presets { get; set; } = new();
}

// ───────────── Archivio (preset salvati in %AppData%) ─────────────

public class Store
{
    static readonly string Dir =
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WSiteFolderStart");
    static readonly string FilePath = System.IO.Path.Combine(Dir, "presets.json");
    static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public List<Preset> Presets { get; private set; } = new();
    public Guid SelectedId { get; set; }
    public event Action? Changed;

    public Preset Selected => Presets.FirstOrDefault(p => p.Id == SelectedId) ?? Presets[0];

    public static Store Load()
    {
        var s = new Store();
        try
        {
            if (File.Exists(FilePath))
            {
                var data = JsonSerializer.Deserialize<StoreData>(File.ReadAllText(FilePath));
                if (data != null && data.Presets.Count > 0)
                {
                    s.Presets = data.Presets;
                    s.SelectedId = data.SelectedId;
                }
            }
        }
        catch { /* file rovinato: si riparte dal preset predefinito */ }

        if (s.Presets.Count == 0) s.Presets.Add(Preset.Default());
        if (!s.Presets.Any(p => p.Id == s.SelectedId)) s.SelectedId = s.Presets[0].Id;
        return s;
    }

    void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var data = new StoreData { SelectedId = SelectedId, Presets = Presets };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(data, Opts));
        }
        catch { /* il salvataggio è best-effort */ }
    }

    /// <summary>Salva e avvisa chi ascolta (la finestra principale) che qualcosa è cambiato.</summary>
    public void Touch()
    {
        Save();
        Changed?.Invoke();
    }

    public int Add()
    {
        Presets.Add(new Preset
        {
            Name = "Nuovo preset",
            Items = new List<Node> { Node.NewFolder("src"), Node.NewFile("README.md", "# {{nome}}\n") }
        });
        Touch();
        return Presets.Count - 1;
    }

    public int Duplicate(int index)
    {
        var copy = JsonSerializer.Deserialize<Preset>(JsonSerializer.Serialize(Presets[index]))!;
        copy.Id = Guid.NewGuid();
        copy.Name += " copia";
        Presets.Add(copy);
        Touch();
        return Presets.Count - 1;
    }

    public void Delete(int index)
    {
        Presets.RemoveAt(index);
        if (Presets.Count == 0) Presets.Add(Preset.Default());
        if (!Presets.Any(p => p.Id == SelectedId)) SelectedId = Presets[0].Id;
        Touch();
    }
}

// ───────────── IDE ─────────────

public record InstalledIde(string Name, string Path);

public static class Ide
{
    /// <summary>IDE conosciuti che risultano installati su questo PC.</summary>
    public static List<InstalledIde> Installed()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var list = new List<InstalledIde>();

        void Add(string name, params string[] candidates)
        {
            foreach (var c in candidates)
            {
                if (File.Exists(c)) { list.Add(new InstalledIde(name, c)); return; }
            }
        }

        Add("Visual Studio Code",
            System.IO.Path.Combine(local, "Programs", "Microsoft VS Code", "Code.exe"),
            System.IO.Path.Combine(pf, "Microsoft VS Code", "Code.exe"));
        Add("Cursor", System.IO.Path.Combine(local, "Programs", "cursor", "Cursor.exe"));
        Add("Windsurf", System.IO.Path.Combine(local, "Programs", "Windsurf", "Windsurf.exe"));
        Add("Zed", System.IO.Path.Combine(local, "Programs", "Zed", "Zed.exe"));
        Add("Sublime Text", System.IO.Path.Combine(pf, "Sublime Text", "sublime_text.exe"));
        Add("Notepad++",
            System.IO.Path.Combine(pf, "Notepad++", "notepad++.exe"),
            System.IO.Path.Combine(pf86, "Notepad++", "notepad++.exe"));

        var web = FindJetBrains("WebStorm", "webstorm64.exe", System.IO.Path.Combine(pf, "JetBrains"), System.IO.Path.Combine(local, "Programs"));
        if (web != null) list.Add(new InstalledIde("WebStorm", web));
        var php = FindJetBrains("PhpStorm", "phpstorm64.exe", System.IO.Path.Combine(pf, "JetBrains"), System.IO.Path.Combine(local, "Programs"));
        if (php != null) list.Add(new InstalledIde("PhpStorm", php));

        return list;
    }

    static string? FindJetBrains(string product, string exe, params string[] roots)
    {
        foreach (var root in roots)
        {
            if (!Directory.Exists(root)) continue;
            foreach (var dir in Directory.GetDirectories(root, product + "*").OrderByDescending(d => d))
            {
                var p = System.IO.Path.Combine(dir, "bin", exe);
                if (File.Exists(p)) return p;
            }
        }
        return null;
    }

    public static string NameOf(string path)
    {
        var known = Installed().FirstOrDefault(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase));
        return known?.Name ?? System.IO.Path.GetFileNameWithoutExtension(path);
    }

    /// <summary>Apre la cartella nell'IDE indicato.</summary>
    public static void Open(string idePath, string folder)
    {
        var psi = new ProcessStartInfo(idePath) { UseShellExecute = false, WorkingDirectory = folder };
        psi.ArgumentList.Add(folder);
        Process.Start(psi);
    }
}

// ───────────── Creazione cartelle ─────────────

public static class Builder
{
    /// <summary>Sostituisce i segnaposto {{nome}}, {{data}} e {{anno}}.</summary>
    public static string Expand(string text, string name) =>
        text.Replace("{{nome}}", name)
            .Replace("{{data}}", DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("it-IT")))
            .Replace("{{anno}}", DateTime.Now.Year.ToString(CultureInfo.InvariantCulture));

    /// <summary>Toglie i caratteri non ammessi nei nomi di file e cartelle.</summary>
    public static string Safe(string text)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var cleaned = string.Concat(text.Trim().Select(c => invalid.Contains(c) ? '-' : c));
        return cleaned.TrimEnd('.', ' ');
    }

    /// <summary>Trasforma il percorso scritto nel preset in un percorso reale (~, %DESKTOP%, %DOCUMENTS%, variabili d'ambiente).</summary>
    public static string ResolveDestination(string? destination)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var d = (destination ?? "").Trim();
        if (d.Length == 0) return desktop;

        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        d = d.Replace("%DESKTOP%", desktop, StringComparison.OrdinalIgnoreCase)
             .Replace("%DOCUMENTS%", docs, StringComparison.OrdinalIgnoreCase);

        if (d == "~" || d.StartsWith("~\\") || d.StartsWith("~/"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            d = System.IO.Path.Combine(home, d.Substring(1).TrimStart('\\', '/'));
        }
        return Environment.ExpandEnvironmentVariables(d);
    }

    /// <summary>Crea la cartella principale e tutta la struttura del preset. Non sovrascrive file esistenti.</summary>
    public static string Create(string name, Preset preset)
    {
        var root = System.IO.Path.Combine(ResolveDestination(preset.Destination), name);
        Directory.CreateDirectory(root);
        Build(preset.Items, root, name);
        return root;
    }

    static void Build(List<Node> nodes, string dir, string name)
    {
        foreach (var node in nodes)
        {
            var nodeName = Safe(Expand(node.Name, name));
            if (nodeName.Length == 0) continue;
            var path = System.IO.Path.Combine(dir, nodeName);
            if (node.IsFolder)
            {
                Directory.CreateDirectory(path);
                Build(node.Children, path, name);
            }
            else if (!File.Exists(path))
            {
                File.WriteAllText(path, Expand(node.Content, name));
            }
        }
    }
}

import Foundation
import AppKit

// MARK: - Modelli

/// Un elemento della struttura: cartella (con figli) oppure file (con contenuto).
struct Node: Codable, Identifiable, Hashable {
    var id = UUID()
    var name: String
    var isFolder: Bool
    var content: String = ""
    var children: [Node] = []

    static func folder(_ name: String, _ children: [Node] = []) -> Node {
        Node(name: name, isFolder: true, children: children)
    }
    static func file(_ name: String, _ content: String = "") -> Node {
        Node(name: name, isFolder: false, content: content)
    }
}

/// Un preset: nome, cartella di destinazione e struttura da creare dentro la cartella principale.
struct Preset: Codable, Identifiable, Hashable {
    var id = UUID()
    var name: String
    var destination: String
    var items: [Node]
    var idePath: String? = nil   // IDE con cui aprire il progetto (percorso dell'app)

    static func defaultPreset() -> Preset {
        Preset(name: "Sito Web", destination: "~/Desktop", items: [
            .folder("Html", [.file("index.html", """
            <!DOCTYPE html>
            <html lang="it">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>{{nome}}</title>
              <link rel="stylesheet" href="../Css/style.css">
            </head>
            <body>
              <h1>{{nome}}</h1>
              <script src="../Js/script.js"></script>
            </body>
            </html>

            """)]),
            .folder("Css", [.file("style.css", "/* Stili di {{nome}} */\n")]),
            .folder("Php", [.file("login.php", "<?php\n// Login di {{nome}}\n")]),
            .folder("Js", [.file("script.js", "// Script di {{nome}}\n")]),
            .folder("Img"),
        ])
    }
}

// MARK: - IDE

struct InstalledIDE: Identifiable {
    let name: String
    let path: String
    var id: String { path }
}

enum IDE {
    static let known: [(name: String, bundle: String)] = [
        ("Visual Studio Code", "com.microsoft.VSCode"),
        ("Cursor", "com.todesktop.230313mzl4w4u92"),
        ("Windsurf", "com.exafunction.windsurf"),
        ("Zed", "dev.zed.Zed"),
        ("Sublime Text", "com.sublimetext.4"),
        ("WebStorm", "com.jetbrains.WebStorm"),
        ("PhpStorm", "com.jetbrains.PhpStorm"),
        ("Nova", "com.panic.Nova"),
        ("BBEdit", "com.barebones.bbedit"),
    ]

    /// IDE conosciuti che risultano installati su questo Mac.
    static func installed() -> [InstalledIDE] {
        known.compactMap { k in
            guard let url = NSWorkspace.shared.urlForApplication(withBundleIdentifier: k.bundle) else { return nil }
            return InstalledIDE(name: k.name, path: url.path)
        }
    }

    static func displayName(path: String) -> String {
        FileManager.default.displayName(atPath: path).replacingOccurrences(of: ".app", with: "")
    }
}

// MARK: - Archivio (preset salvati in Application Support)

final class Store: ObservableObject {
    @Published var presets: [Preset] { didSet { save() } }
    @Published var selectedID: UUID { didSet { UserDefaults.standard.set(selectedID.uuidString, forKey: "selectedPreset") } }

    private let fileURL: URL

    init() {
        let dir = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("WSiteFolderStart", isDirectory: true)
        try? FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        let url = dir.appendingPathComponent("presets.json")
        fileURL = url

        var loaded: [Preset] = []
        if let data = try? Data(contentsOf: url),
           let decoded = try? JSONDecoder().decode([Preset].self, from: data) {
            loaded = decoded
        }
        if loaded.isEmpty { loaded = [Preset.defaultPreset()] }

        // Migrazione: l'IDE una volta era unico per tutta l'app, ora è per preset.
        let legacyIDE = UserDefaults.standard.string(forKey: "idePath") ?? ""
        if !legacyIDE.isEmpty {
            for i in loaded.indices where loaded[i].idePath == nil { loaded[i].idePath = legacyIDE }
            if let data = try? JSONEncoder().encode(loaded) { try? data.write(to: url, options: .atomic) }
            UserDefaults.standard.removeObject(forKey: "idePath")
        }
        presets = loaded

        var sel = loaded[0].id
        if let s = UserDefaults.standard.string(forKey: "selectedPreset"),
           let u = UUID(uuidString: s),
           loaded.contains(where: { $0.id == u }) {
            sel = u
        }
        selectedID = sel
    }

    var selected: Preset {
        presets.first(where: { $0.id == selectedID }) ?? presets[0]
    }

    private func save() {
        if let data = try? JSONEncoder().encode(presets) {
            try? data.write(to: fileURL, options: .atomic)
        }
    }

    @discardableResult
    func addPreset() -> UUID {
        let p = Preset(name: "Nuovo preset", destination: "~/Desktop",
                       items: [.folder("src"), .file("README.md", "# {{nome}}\n")])
        presets.append(p)
        return p.id
    }

    func duplicate(_ id: UUID) -> UUID? {
        guard var copy = presets.first(where: { $0.id == id }) else { return nil }
        copy.id = UUID()
        copy.name += " copia"
        presets.append(copy)
        return copy.id
    }

    func delete(_ id: UUID) {
        presets.removeAll { $0.id == id }
        if presets.isEmpty { presets = [Preset.defaultPreset()] }
        if !presets.contains(where: { $0.id == selectedID }) { selectedID = presets[0].id }
    }
}

// MARK: - Creazione cartelle

enum Builder {
    /// Sostituisce i segnaposto {{nome}}, {{data}} e {{anno}}.
    static func expand(_ text: String, name: String) -> String {
        let df = DateFormatter()
        df.locale = Locale(identifier: "it_IT")
        df.dateFormat = "dd/MM/yyyy"
        let year = String(Calendar.current.component(.year, from: Date()))
        return text
            .replacingOccurrences(of: "{{nome}}", with: name)
            .replacingOccurrences(of: "{{data}}", with: df.string(from: Date()))
            .replacingOccurrences(of: "{{anno}}", with: year)
    }

    /// Toglie i caratteri non ammessi nei nomi di file.
    static func safe(_ text: String) -> String {
        text.replacingOccurrences(of: "/", with: "-")
            .replacingOccurrences(of: ":", with: "-")
            .trimmingCharacters(in: .whitespaces)
    }

    static func destinationURL(for preset: Preset) -> URL {
        let raw = preset.destination.trimmingCharacters(in: .whitespaces)
        let path = NSString(string: raw.isEmpty ? "~/Desktop" : raw).expandingTildeInPath
        return URL(fileURLWithPath: path, isDirectory: true)
    }

    /// Crea la cartella principale e tutta la struttura del preset. Non sovrascrive file esistenti.
    static func create(named name: String, preset: Preset) throws -> URL {
        let fm = FileManager.default
        let root = destinationURL(for: preset).appendingPathComponent(name, isDirectory: true)
        try fm.createDirectory(at: root, withIntermediateDirectories: true)
        try build(preset.items, in: root, name: name)
        return root
    }

    private static func build(_ nodes: [Node], in dir: URL, name: String) throws {
        let fm = FileManager.default
        for node in nodes {
            let nodeName = safe(expand(node.name, name: name))
            if nodeName.isEmpty || nodeName == "." || nodeName == ".." { continue }
            let url = dir.appendingPathComponent(nodeName, isDirectory: node.isFolder)
            if node.isFolder {
                try fm.createDirectory(at: url, withIntermediateDirectories: true)
                try build(node.children, in: url, name: name)
            } else if !fm.fileExists(atPath: url.path) {
                try expand(node.content, name: name).write(to: url, atomically: true, encoding: .utf8)
            }
        }
    }
}

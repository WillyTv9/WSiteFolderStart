import SwiftUI
import AppKit
import UniformTypeIdentifiers

// MARK: - Finestra "Preset e IDE"

struct PresetsWindow: View {
    @EnvironmentObject var store: Store
    @State private var selection: UUID?

    var body: some View {
        HStack(spacing: 0) {
            VStack(spacing: 0) {
                List(selection: $selection) {
                    ForEach(store.presets) { p in
                        Label(p.name, systemImage: "square.stack.3d.up")
                    }
                }
                .listStyle(.sidebar)

                Divider()
                HStack(spacing: 14) {
                    Button { selection = store.addPreset() } label: { Image(systemName: "plus") }
                        .help("Nuovo preset")
                    Button {
                        if let s = selection, let n = store.duplicate(s) { selection = n }
                    } label: { Image(systemName: "doc.on.doc") }
                        .help("Duplica preset")
                    Button {
                        if let s = selection {
                            store.delete(s)
                            selection = store.presets.first?.id
                        }
                    } label: { Image(systemName: "minus") }
                        .help("Elimina preset")
                    Spacer()
                }
                .buttonStyle(.borderless)
                .padding(10)
            }
            .frame(width: 230)

            Divider()

            Group {
                if let sel = selection, let i = store.presets.firstIndex(where: { $0.id == sel }) {
                    PresetEditor(preset: $store.presets[i]).id(sel)
                } else {
                    Text("Seleziona o crea un preset")
                        .foregroundStyle(.secondary)
                        .frame(maxWidth: .infinity, maxHeight: .infinity)
                }
            }
        }
        .frame(minWidth: 840, minHeight: 560)
        .onAppear { if selection == nil { selection = store.selectedID } }
    }
}

// MARK: - IDE del preset

struct IDEPicker: View {
    @Binding var path: String?

    private var current: String { path ?? "" }

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text("IDE con cui aprire il progetto").font(.headline)
            Menu {
                Button { path = nil } label: { item("Nessuno", current.isEmpty) }
                ForEach(IDE.installed()) { ide in
                    Button { path = ide.path } label: { item(ide.name, current == ide.path) }
                }
                Divider()
                Button("Altra app…") { chooseApp() }
            } label: {
                Text(current.isEmpty ? "Nessuno" : IDE.displayName(path: current))
            }
            .fixedSize()
            Text("Se è impostato, dopo «Scatena» l'app apre la cartella principale in questo IDE (non nel browser) e si chiude.")
                .font(.caption).foregroundStyle(.secondary)
        }
    }

    @ViewBuilder
    private func item(_ title: String, _ selected: Bool) -> some View {
        if selected { Label(title, systemImage: "checkmark") } else { Text(title) }
    }

    private func chooseApp() {
        let panel = NSOpenPanel()
        panel.allowedContentTypes = [.application]
        panel.directoryURL = URL(fileURLWithPath: "/Applications")
        panel.canChooseDirectories = false
        panel.prompt = "Scegli"
        if panel.runModal() == .OK, let url = panel.url { path = url.path }
    }
}

// MARK: - Editor di un preset

struct PresetEditor: View {
    @Binding var preset: Preset

    private var resolvedPath: String {
        Builder.destinationURL(for: preset).path
    }

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 22) {
                VStack(alignment: .leading, spacing: 6) {
                    Text("Nome del preset").font(.headline)
                    TextField("Nome", text: $preset.name).textFieldStyle(.roundedBorder)
                }

                VStack(alignment: .leading, spacing: 6) {
                    Text("Dove creare la cartella").font(.headline)
                    HStack {
                        TextField("~/Desktop", text: $preset.destination)
                            .textFieldStyle(.roundedBorder)
                            .font(.system(.body, design: .monospaced))
                        Button("Scegli…") { chooseFolder() }
                    }
                    Text("Verrà creata in: \(resolvedPath)/NomeScelto")
                        .font(.caption).foregroundStyle(.secondary)
                }

                IDEPicker(path: $preset.idePath)

                VStack(alignment: .leading, spacing: 10) {
                    HStack {
                        Text("Struttura").font(.headline)
                        Spacer()
                        Menu {
                            Button("Nuova cartella") { preset.items.append(.folder("nuova-cartella")) }
                            Button("Nuovo file") { preset.items.append(.file("nuovo-file.txt")) }
                        } label: {
                            Label("Aggiungi", systemImage: "plus")
                        }
                        .menuStyle(.borderlessButton)
                        .fixedSize()
                    }

                    HStack(spacing: 8) {
                        Image(systemName: "folder.fill").foregroundStyle(Color.accentColor).frame(width: 18)
                        Text("{{nome}}").font(.system(.body, design: .monospaced))
                        Text("cartella principale").foregroundStyle(.secondary)
                    }

                    VStack(alignment: .leading, spacing: 6) {
                        ForEach($preset.items) { $node in
                            NodeEditor(node: $node, onDelete: {
                                preset.items.removeAll { $0.id == node.id }
                            })
                        }
                    }
                    .padding(.leading, 22)
                }

                Text("Nei nomi e nel contenuto dei file puoi usare {{nome}} (il nome scritto nella finestra principale), {{data}} e {{anno}}. Una cartella senza elementi viene creata vuota, un file senza contenuto viene creato vuoto.")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
            .padding(24)
            .frame(maxWidth: .infinity, alignment: .leading)
        }
    }

    private func chooseFolder() {
        let panel = NSOpenPanel()
        panel.canChooseFiles = false
        panel.canChooseDirectories = true
        panel.canCreateDirectories = true
        panel.prompt = "Scegli"
        if panel.runModal() == .OK, let url = panel.url {
            let home = NSHomeDirectory()
            if url.path == home {
                preset.destination = "~"
            } else if url.path.hasPrefix(home + "/") {
                preset.destination = "~" + String(url.path.dropFirst(home.count))
            } else {
                preset.destination = url.path
            }
        }
    }
}

// MARK: - Riga ricorsiva (cartella o file)

struct NodeEditor: View {
    @Binding var node: Node
    var onDelete: () -> Void
    @State private var showContent = false

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack(spacing: 8) {
                Image(systemName: node.isFolder ? "folder.fill" : "doc.text")
                    .foregroundStyle(node.isFolder ? Color.accentColor : Color.secondary)
                    .frame(width: 18)
                TextField("nome", text: $node.name).textFieldStyle(.roundedBorder)

                if node.isFolder {
                    Menu {
                        Button("Nuova cartella") { node.children.append(.folder("nuova-cartella")) }
                        Button("Nuovo file") { node.children.append(.file("nuovo-file.txt")) }
                    } label: {
                        Image(systemName: "plus")
                    }
                    .menuStyle(.borderlessButton)
                    .fixedSize()
                } else {
                    Button { showContent.toggle() } label: {
                        Image(systemName: showContent ? "chevron.up" : "text.alignleft")
                    }
                    .buttonStyle(.borderless)
                    .help("Modifica il contenuto del file")
                }

                Button(role: .destructive, action: onDelete) { Image(systemName: "trash") }
                    .buttonStyle(.borderless)
            }

            if node.isFolder {
                VStack(alignment: .leading, spacing: 6) {
                    ForEach($node.children) { $child in
                        NodeEditor(node: $child, onDelete: {
                            node.children.removeAll { $0.id == child.id }
                        })
                    }
                }
                .padding(.leading, 22)
            } else if showContent {
                TextEditor(text: $node.content)
                    .font(.system(.body, design: .monospaced))
                    .scrollContentBackground(.hidden)
                    .padding(6)
                    .frame(height: 140)
                    .background(Color.primary.opacity(0.06), in: RoundedRectangle(cornerRadius: 8))
                    .padding(.leading, 26)
            }
        }
    }
}
